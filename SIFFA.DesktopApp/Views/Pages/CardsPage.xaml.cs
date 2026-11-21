using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SIFFA.Core.Services.EmployeeService;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Asistente de 3 pasos: logo → previsualización (QR o barras) → formato y generación del .zip.</summary>
    public partial class CardsPage : UserControl
    {
        private const long MaxLogoSize = 2 * 1024 * 1024;

        private readonly IServiceProvider _services;
        private readonly string[] _titles = { "Subir logo", "Previsualizar tarjetas", "Confirmar y generar" };

        private int _step;
        private byte[]? _logo;
        private InvitationCardData? _sample;
        private bool _hasPreview;
        private bool _busy;
        private bool _finished;
        private string? _savedPath;

        public CardsPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            Refresh();
        }

        private CardCodeType CodeType => RbBarcode.IsChecked == true ? CardCodeType.Barcode : CardCodeType.Qr;
        private CardFormat Format => RbPdf.IsChecked == true ? CardFormat.Pdf : CardFormat.Png;

        private bool CanContinue => _step switch
        {
            0 => _logo is not null && !_busy,
            1 => _sample is not null && _hasPreview && !_busy,
            _ => false,
        };

        // ---------- pantalla ----------

        private void Refresh()
        {
            Step0.Visibility = !_finished && _step == 0 ? Visibility.Visible : Visibility.Collapsed;
            Step1.Visibility = !_finished && _step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = !_finished && _step == 2 ? Visibility.Visible : Visibility.Collapsed;
            StepDone.Visibility = _finished ? Visibility.Visible : Visibility.Collapsed;
            NavBar.Visibility = Steps.Visibility = _finished ? Visibility.Collapsed : Visibility.Visible;

            BtnPrev.Visibility = _step > 0 ? Visibility.Visible : Visibility.Hidden;
            BtnPrev.IsEnabled = !_busy;
            BtnNext.Content = _step == 2 ? (_busy ? "Generando…" : "Terminar y generar") : "Siguiente";
            BtnNext.IsEnabled = _step == 2 ? !_busy : CanContinue;

            Steps.Children.Clear();
            for (var i = 0; i < _titles.Length; i++)
            {
                var active = i == _step;
                var done = i < _step;
                Steps.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(12, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = (Brush)FindResource(active ? "BrushCyan" : "BrushNavy"),
                    Child = new TextBlock
                    {
                        Text = $"{(done ? "✓" : (i + 1).ToString())}  {_titles[i]}",
                        FontSize = 12,
                        FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = (Brush)FindResource(active ? "BrushNight" : done ? "BrushCyan" : "BrushLavender"),
                    },
                });
            }
        }

        private static BitmapImage ToImage(byte[] bytes)
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        // ---------- paso 1: logo ----------

        private async void OnPickLogo(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp", Title = "Logo de la empresa" };
            if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

            _busy = true;
            TxtLogoError.Visibility = Visibility.Collapsed;
            LogoInfo.Visibility = Visibility.Collapsed;
            _logo = null;
            _hasPreview = false;
            Refresh();

            try
            {
                var info = new FileInfo(dialog.FileName);
                if (info.Length > MaxLogoSize)
                    throw new InvalidOperationException("El logo pesa más de 2 MB.");

                var bytes = await File.ReadAllBytesAsync(dialog.FileName);

                // Se valida que de verdad sea una imagen que se pueda dibujar.
                using (var scope = _services.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IEmployeeService>();
                    var probe = new InvitationCardData { FullName = "x", EmployeeNumber = "0", Area = "x", EmployeeType = "x" };
                    if (service.RenderCardPreview(probe, bytes) is null)
                        throw new InvalidOperationException("El archivo no es una imagen válida.");
                }

                _logo = bytes;
                try { LogoImage.Source = ToImage(bytes); } catch { LogoImage.Source = null; } // WebP no siempre se puede mostrar aquí
                TxtLogoName.Text = info.Name;
                LogoInfo.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                TxtLogoError.Text = $"No se pudo cargar el logo: {ex.GetBaseException().Message}";
                TxtLogoError.Visibility = Visibility.Visible;
            }
            finally
            {
                _busy = false;
                Refresh();
            }
        }

        // ---------- paso 2: previsualización ----------

        private async Task LoadPreviewAsync()
        {
            _busy = true;
            _hasPreview = false;
            TxtPreviewError.Visibility = Visibility.Collapsed;
            Refresh();
            try
            {
                using var scope = _services.CreateScope();
                _sample = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetCardSampleAsync();
                TxtNoSample.Visibility = _sample is null ? Visibility.Visible : Visibility.Collapsed;
                PreviewPanel.Visibility = _sample is null ? Visibility.Collapsed : Visibility.Visible;
                if (_sample is not null)
                {
                    TxtSample.Text = $"Ejemplo con {_sample.FullName} (No. {_sample.EmployeeNumber}).";
                    RenderPreview();
                }
            }
            catch (Exception ex)
            {
                TxtPreviewError.Text = $"No se pudo cargar la muestra: {ex.GetBaseException().Message}";
                TxtPreviewError.Visibility = Visibility.Visible;
            }
            finally
            {
                _busy = false;
                Refresh();
            }
        }

        private void RenderPreview()
        {
            _hasPreview = false;
            TxtPreviewError.Visibility = Visibility.Collapsed;
            if (_sample is null || _logo is null) return;

            using var scope = _services.CreateScope();
            var png = scope.ServiceProvider.GetRequiredService<IEmployeeService>().RenderCardPreview(_sample, _logo, CodeType);
            if (png is null)
            {
                PreviewImage.Source = null;
                TxtPreviewError.Text = "No se pudo dibujar la tarjeta con ese logo.";
                TxtPreviewError.Visibility = Visibility.Visible;
                return;
            }

            PreviewImage.Source = ToImage(png);
            _hasPreview = true;
        }

        private void OnCodeChanged(object sender, RoutedEventArgs e)
        {
            if (_sample is null || RbQr is null || RbBarcode is null) return;
            RenderPreview(); // la muestra ya está cargada: solo se vuelve a dibujar
            Refresh();
        }

        // ---------- navegación ----------

        private void OnPrev(object sender, RoutedEventArgs e)
        {
            if (_step == 0 || _busy) return;
            _step--;
            Refresh();
        }

        private async void OnNext(object sender, RoutedEventArgs e)
        {
            if (_step == 0)
            {
                if (!CanContinue) return;
                _step = 1;
                Refresh();
                await LoadPreviewAsync();
            }
            else if (_step == 1)
            {
                if (!CanContinue) return;
                _step = 2;
                TxtCodeSummary.Text = $"Código en la tarjeta: {(CodeType == CardCodeType.Qr ? "QR" : "código de barras")}.";
                TxtError.Visibility = Visibility.Collapsed;
                Refresh();
            }
            else
            {
                await GenerateAsync();
            }
        }

        // ---------- paso 3: generar ----------

        private async Task GenerateAsync()
        {
            if (_logo is null) return;

            var save = new SaveFileDialog
            {
                Title = "Guardar tarjetas",
                Filter = "Archivo zip (*.zip)|*.zip",
                FileName = $"Tarjetas-{DateTime.Now:yyyyMMdd-HHmm}.zip",
            };
            if (save.ShowDialog(Window.GetWindow(this)) != true) return;

            _busy = true;
            TxtError.Visibility = Visibility.Collapsed;
            Refresh();
            try
            {
                using var scope = _services.CreateScope();
                var progress = new Progress<CardsProgress>(p => BtnNext.Content = $"Generando… {p.Done} de {p.Total}");

                // El zip se escribe directo al archivo, por lotes: no se junta todo en memoria.
                CardsZipResult result;
                await using (var file = new FileStream(save.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    result = await scope.ServiceProvider.GetRequiredService<IEmployeeService>()
                        .GenerateCardsZipAsync(_logo, Format, CodeType, file, progress);

                if (!result.IsSuccess)
                {
                    TryDelete(save.FileName);
                    TxtError.Text = result.Message;
                    TxtError.Visibility = Visibility.Visible;
                    return;
                }

                _savedPath = save.FileName;
                TxtDone.Text = $"{result.Message} Se guardó {Path.GetFileName(save.FileName)}.";
                _finished = true;
            }
            catch (Exception ex)
            {
                TryDelete(save.FileName);
                TxtError.Text = $"No se pudo generar o guardar: {ex.GetBaseException().Message}";
                TxtError.Visibility = Visibility.Visible;
            }
            finally
            {
                _busy = false;
                Refresh();
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* si no se puede borrar, se deja */ }
        }

        private void OnShowFile(object sender, RoutedEventArgs e)
        {
            if (_savedPath is not null && File.Exists(_savedPath))
                Process.Start("explorer.exe", $"/select,\"{_savedPath}\"");
        }

        private void OnStartOver(object sender, RoutedEventArgs e)
        {
            _finished = false;
            _step = 0;
            TxtError.Visibility = Visibility.Collapsed;
            Refresh();
        }
    }
}
