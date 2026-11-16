using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SIFFA.Core.Models.Custom.Response;
using SIFFA.Core.Services.AttendanceService;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Reporte de asistencia: dentro, no han entrado y ya salieron. Se refresca solo cada 30 segundos.</summary>
    public partial class AttendancePage : UserControl
    {
        private readonly IServiceProvider _services;
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
        private AttendanceReport? _report;
        private bool _loading;

        public AttendancePage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            _timer.Tick += async (_, _) => await LoadAsync();
            Loaded += async (_, _) => { _timer.Start(); await LoadAsync(); };
            Unloaded += (_, _) => _timer.Stop();
        }

        private AttendanceStatus? Filter =>
            RbInside.IsChecked == true ? AttendanceStatus.Inside
            : RbPending.IsChecked == true ? AttendanceStatus.NotEntered
            : RbExited.IsChecked == true ? AttendanceStatus.Exited
            : null;

        private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

        private void OnFilter(object sender, RoutedEventArgs e) => ShowRows();

        private async Task LoadAsync()
        {
            if (_loading) return;
            _loading = true;
            BtnRefresh.IsEnabled = false;
            try
            {
                using var scope = _services.CreateScope();
                _report = await scope.ServiceProvider.GetRequiredService<IAttendanceService>().GetReportAsync();

                TxtError.Visibility = Visibility.Collapsed;
                TxtUpdated.Text = $"Actualizado {_report.GeneratedAt:HH:mm:ss} · {_report.Total} empleados registrados";
                LblAll.Text = $"Todos ({_report.Total})";
                LblInside.Text = $"Dentro ({_report.Inside})";
                LblPending.Text = $"No han entrado ({_report.NotEntered})";
                LblExited.Text = $"Ya salieron ({_report.Exited})";
                ShowRows();
            }
            catch (Exception ex)
            {
                TxtError.Text = $"No se pudo cargar el reporte: {ex.GetBaseException().Message}";
                TxtError.Visibility = Visibility.Visible;
            }
            finally
            {
                _loading = false;
                BtnRefresh.IsEnabled = true;
            }
        }

        private List<AttendanceRow> VisibleRows()
        {
            if (_report is null) return new();
            var filter = Filter;
            return filter is null ? _report.Employees : _report.Employees.Where(r => r.Status == filter).ToList();
        }

        private void ShowRows()
        {
            if (Grid is null) return;
            Grid.ItemsSource = VisibleRows();
        }

        // ---------- exportar ----------

        private void OnExport(object sender, RoutedEventArgs e)
        {
            var rows = VisibleRows();
            if (rows.Count == 0)
            {
                MessageBox.Show(Window.GetWindow(this), "No hay empleados en este filtro para exportar.", "Reporte de asistencia",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var save = new SaveFileDialog
            {
                Title = "Guardar reporte de asistencia",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"Asistencia-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
            };
            if (save.ShowDialog(Window.GetWindow(this)) != true) return;

            try
            {
                using var book = new XLWorkbook();
                var sheet = book.Worksheets.Add("Asistencia");
                string[] headers = ["Número", "Empleado", "Correo", "Área", "Tipo", "Estado", "Primera entrada", "Último movimiento"];
                for (var c = 0; c < headers.Length; c++)
                    sheet.Cell(1, c + 1).Value = headers[c];
                sheet.Row(1).Style.Font.Bold = true;

                for (var i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    var row = i + 2;
                    sheet.Cell(row, 1).Value = r.EmployeeNumber;
                    sheet.Cell(row, 2).Value = r.FullName;
                    sheet.Cell(row, 3).Value = r.Email;
                    sheet.Cell(row, 4).Value = r.Area;
                    sheet.Cell(row, 5).Value = r.EmployeeType;
                    sheet.Cell(row, 6).Value = r.StatusText;
                    if (r.FirstEntry is { } first) sheet.Cell(row, 7).Value = first;
                    if (r.LastMovement is { } last) sheet.Cell(row, 8).Value = last;
                }
                sheet.Columns(7, 8).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                sheet.Columns().AdjustToContents();
                book.SaveAs(save.FileName);

                Process.Start(new ProcessStartInfo(save.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                TxtError.Text = $"No se pudo guardar el reporte: {ex.GetBaseException().Message}";
                TxtError.Visibility = Visibility.Visible;
            }
        }
    }
}
