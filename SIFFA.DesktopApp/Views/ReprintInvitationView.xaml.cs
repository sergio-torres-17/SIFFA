using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SIFFA.Core.Services.AccessControlService;
using SIFFA.Core.Services.EmployeeService;
using SIFFA.DesktopApp.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Busca a un empleado (nombre, número o correo) y reimprime su invitación en una impresora térmica.</summary>
    public partial class ReprintInvitationView : Window
    {
        private const long MaxLogoSize = 2 * 1024 * 1024;

        private readonly IServiceProvider _services;
        private readonly ReprintSettings _settings = ReprintSettings.Load();
        private byte[]? _logo = ReprintSettings.LoadLogo();
        private InvitationCardData? _card;
        private BitmapSource? _image;   // la invitación tal como la dibuja el sistema
        private bool _loading = true;
        private bool _busy;

        public ReprintInvitationView(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            RbBarcode.IsChecked = _settings.Barcode;
            RbQr.IsChecked = !_settings.Barcode;
            RbVertical.IsChecked = _settings.Vertical;
            RbHorizontal.IsChecked = !_settings.Vertical;
            _loading = false;

            ShowLogo();
            Loaded += (_, _) => TxtTerm.Focus();
        }

        private CardCodeType CodeType => RbBarcode.IsChecked == true ? CardCodeType.Barcode : CardCodeType.Qr;
        private bool Vertical => RbVertical.IsChecked == true;

        private sealed record Match(EmployeeSearchResult Item)
        {
            public string FullName => Item.FullName;
            public string Detail => $"No. {Item.EmployeeNumber} · {Item.Area} · {Item.Email}";
        }

        // ---------- búsqueda ----------

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtStatus is null) return;
            TxtStatus.Visibility = Visibility.Collapsed;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not Key.Enter) return;
            e.Handled = true;
            Search();
        }

        private async void Search()
        {
            if (_busy) return;
            var term = TxtTerm.Text.Trim();
            if (term.Length == 0) return;

            _busy = true;
            MatchesPanel.Visibility = Visibility.Collapsed;
            PreviewPanel.Visibility = Visibility.Collapsed;
            _card = null;
            try
            {
                List<EmployeeSearchResult> found;
                using (var scope = _services.CreateScope())
                    found = scope.ServiceProvider.GetRequiredService<IAccessControlService>().SearchEmployees(term);

                if (found.Count == 0)
                {
                    ShowStatus("No se encontró ningún empleado con ese dato.", ok: false);
                }
                else if (found.Count == 1)
                {
                    await LoadCard(found[0].EmployeeNumber);
                }
                else
                {
                    Matches.ItemsSource = found.Select(f => new Match(f)).ToList();
                    MatchesPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo buscar: {ex.GetBaseException().Message}", ok: false);
            }
            finally
            {
                _busy = false;
            }
        }

        private async void OnPick(object sender, RoutedEventArgs e)
        {
            if (_busy || ((FrameworkElement)sender).Tag is not Match match) return;
            _busy = true;
            MatchesPanel.Visibility = Visibility.Collapsed;
            try { await LoadCard(match.Item.EmployeeNumber); }
            catch (Exception ex) { ShowStatus($"No se pudo cargar la invitación: {ex.GetBaseException().Message}", ok: false); }
            finally { _busy = false; }
        }

        private async Task LoadCard(string employeeNumber)
        {
            InvitationCardData? card;
            using (var scope = _services.CreateScope())
                card = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetCardAsync(employeeNumber);

            if (card is null)
            {
                ShowStatus("No se encontró la invitación de ese empleado.", ok: false);
                return;
            }

            _card = card;

            // La primera vez se pide el logo (se guarda en este equipo).
            if (_logo is null && !PickLogo())
            {
                ShowStatus("Para dibujar la invitación primero elige el logo de la empresa.", ok: false);
                return;
            }

            RenderCard();
        }

        // ---------- dibujo y vista previa ----------

        private void RenderCard()
        {
            if (_card is null || _logo is null) return;

            byte[]? png;
            using (var scope = _services.CreateScope())
                png = scope.ServiceProvider.GetRequiredService<IEmployeeService>().RenderCardPreview(_card, _logo, CodeType);

            if (png is null)
            {
                _image = null;
                PreviewPanel.Visibility = Visibility.Collapsed;
                ShowStatus("No se pudo dibujar la invitación con ese logo. Elige otro.", ok: false);
                return;
            }

            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(png))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            _image = bitmap;

            TxtWho.Text = $"{_card.FullName} · No. {_card.EmployeeNumber}";
            PreviewImage.Source = ThermalPrinter.Prepare(_image, Vertical, highContrast: true); // lo que realmente saldrá en papel
            PreviewPanel.Visibility = Visibility.Visible;
            TxtStatus.Visibility = Visibility.Collapsed;
            BtnPrint.Focus(); // Enter otra vez = imprimir
        }

        private void OnOptionChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _settings.Barcode = RbBarcode.IsChecked == true;
            _settings.Vertical = RbVertical.IsChecked == true;
            _settings.Save();
            RenderCard();
        }

        // ---------- impresión ----------

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            if (_image is null || _card is null) return;
            try
            {
                var printed = ThermalPrinter.Print(_image, Vertical, highContrast: true, $"SIFFA · invitación {_card.EmployeeNumber}");
                if (printed)
                {
                    ShowStatus($"Invitación de {_card.FullName} enviada a la impresora.", ok: true);
                    PreviewPanel.Visibility = Visibility.Collapsed;
                    TxtTerm.Clear();
                    TxtTerm.Focus(); // listo para la siguiente persona
                    _card = null;
                    _image = null;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo imprimir: {ex.GetBaseException().Message}", ok: false);
            }
        }

        // ---------- logo ----------

        private void OnPickLogo(object sender, RoutedEventArgs e)
        {
            if (PickLogo())
                RenderCard();
        }

        /// <summary>Pide el logo, lo valida y lo guarda en este equipo. false si se cancela o no sirve.</summary>
        private bool PickLogo()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Logo de la empresa",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp",
            };
            if (dialog.ShowDialog(this) != true) return false;

            try
            {
                var info = new FileInfo(dialog.FileName);
                if (info.Length > MaxLogoSize)
                    throw new InvalidOperationException("El logo pesa más de 2 MB.");

                var bytes = File.ReadAllBytes(dialog.FileName);

                // Se valida que de verdad sea una imagen que el sistema pueda dibujar.
                using (var scope = _services.CreateScope())
                {
                    var probe = new InvitationCardData { FullName = "x", EmployeeNumber = "0", Area = "x", EmployeeType = "x" };
                    if (scope.ServiceProvider.GetRequiredService<IEmployeeService>().RenderCardPreview(probe, bytes) is null)
                        throw new InvalidOperationException("El archivo no es una imagen válida.");
                }

                _logo = bytes;
                _settings.SaveLogo(bytes, info.Name);
                ShowLogo();
                return true;
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo usar ese logo: {ex.GetBaseException().Message}", ok: false);
                return false;
            }
        }

        private void ShowLogo()
        {
            TxtLogo.Text = _logo is null
                ? "Todavía no hay logo; se pedirá la primera vez que busques una invitación."
                : $"Guardado en este equipo: {_settings.LogoName ?? "logo"}";
            BtnLogo.Content = _logo is null ? "Elegir logo" : "Cambiar logo";
        }

        // ---------- varios ----------

        private void ShowStatus(string message, bool ok)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = Visibility.Visible;
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
    }
}
