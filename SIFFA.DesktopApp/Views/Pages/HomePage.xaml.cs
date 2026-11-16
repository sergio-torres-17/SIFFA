using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.DashboardService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Dashboard: indicadores, gráficas y tablas de accesos. Se refresca solo cada 30 segundos.</summary>
    public partial class HomePage : UserControl
    {
        private const double MaxBarHeight = 150;

        private readonly IServiceProvider _services;
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
        private bool _loading;

        public HomePage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            _timer.Tick += async (_, _) => await LoadAsync();
            Loaded += async (_, _) => { _timer.Start(); await LoadAsync(); };
            Unloaded += (_, _) => _timer.Stop();
        }

        private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            if (_loading) return;
            _loading = true;
            BtnRefresh.IsEnabled = false;
            try
            {
                using var scope = _services.CreateScope();
                var data = await scope.ServiceProvider.GetRequiredService<IDashboardService>().GetSummaryAsync();

                TxtError.Visibility = Visibility.Collapsed;
                TxtNoActions.Visibility = data.HasAccessActions ? Visibility.Collapsed : Visibility.Visible;
                TxtUpdated.Text = $"Fiesta de fin de año · actualizado {DateTime.Now:HH:mm:ss}";

                KTotal.Text = data.TotalEmployees.ToString();
                KEntered.Text = data.Entered.ToString();
                KPending.Text = data.Pending.ToString();
                KInside.Text = data.Inside.ToString();
                KExited.Text = data.Exited.ToString();

                var percent = Percent(data.Entered, data.TotalEmployees);
                TxtAttendance.Text = $"Asistencia: ingresaron {data.Entered} de {data.TotalEmployees}";
                TxtPercent.Text = $"{percent}%";
                BarAttendance.Value = percent;

                var total = Math.Max(1, data.TotalEmployees);
                StatusBars.ItemsSource = new[]
                {
                    new StatusRow("Faltan por ingresar", data.Pending, data.Pending * 100.0 / total, Hex("#FCD34D")),
                    new StatusRow("Dentro", data.Inside, data.Inside * 100.0 / total, (Brush)FindResource("BrushSuccess")),
                    new StatusRow("Egresaron", data.Exited, data.Exited * 100.0 / total, (Brush)FindResource("BrushLavender")),
                };

                var max = Math.Max(1, data.EntriesByHour.Select(h => h.Entries).DefaultIfEmpty(0).Max());
                HourChart.ItemsSource = data.EntriesByHour
                    .Select(h => new HourBar($"{h.Hour:00}", h.Entries, Math.Max(2, h.Entries * MaxBarHeight / max)))
                    .ToList();
                var hasHours = data.EntriesByHour.Count > 0;
                HourChart.Visibility = hasHours ? Visibility.Visible : Visibility.Collapsed;
                TxtNoHours.Visibility = hasHours ? Visibility.Collapsed : Visibility.Visible;

                AreaGrid.ItemsSource = data.ByArea;
                LatestGrid.ItemsSource = data.LatestAccesses;
                var hasAccesses = data.LatestAccesses.Count > 0;
                LatestGrid.Visibility = hasAccesses ? Visibility.Visible : Visibility.Collapsed;
                TxtNoAccesses.Visibility = hasAccesses ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                TxtError.Text = $"No se pudo cargar el dashboard: {ex.GetBaseException().Message}";
                TxtError.Visibility = Visibility.Visible;
            }
            finally
            {
                _loading = false;
                BtnRefresh.IsEnabled = true;
            }
        }

        private static double Percent(int part, int total) => total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);

        private static Brush Hex(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

        public sealed record StatusRow(string Label, int Value, double Percent, Brush Brush);
        public sealed record HourBar(string Label, int Entries, double BarHeight);
    }
}
