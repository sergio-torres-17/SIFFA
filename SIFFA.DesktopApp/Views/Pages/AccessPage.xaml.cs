using SIFFA.Core.Models.Custom.Request.AccessControl;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Registro de ingresos y egresos, con entrega de boletos adicionales.</summary>
    public partial class AccessPage : UserControl
    {
        private readonly IServiceProvider _services;
        private readonly ObservableCollection<Entry> _recent = new();

        public AccessPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            Grid.ItemsSource = _recent;
        }

        public sealed record Entry(DateTime Time, string Number, string Name, AccessType Type, int Delivered)
        {
            public string Display => $"{Name} ({Number})";
            public string Movement => Type == AccessType.Entry ? "Ingreso" : "Egreso";
        }

        private async void OnEntry(object sender, RoutedEventArgs e) => await Run(AccessType.Entry);
        private async void OnExit(object sender, RoutedEventArgs e) => await Run(AccessType.Exit);

        // Modo continuo: al terminar cada empleado se vuelve a abrir la captura para escanear al siguiente
        // sin tocar el ratón. Se sale con "Cerrar".
        private Task Run(AccessType type)
        {
            BtnEntry.IsEnabled = BtnExit.IsEnabled = false;
            var owner = Window.GetWindow(this);
            try
            {
                while (true)
                {
                    var scan = new AccessScanView(_services, type) { Owner = owner };
                    if (scan.ShowDialog() != true || scan.Response is not { } response)
                        break;

                    var delivered = 0;
                    if (response.employeeAdds is { Count: > 0 })
                    {
                        var delivery = new AccessDeliveryView(_services, response) { Owner = owner };
                        if (delivery.ShowDialog() == true)
                            delivered = delivery.DeliveredCount;
                    }

                    _recent.Insert(0, new Entry(DateTime.Now, response.employeeNumber ?? string.Empty,
                        response.employeeName ?? string.Empty, type, delivered));
                    Grid.Visibility = Visibility.Visible;
                    TxtEmpty.Visibility = Visibility.Collapsed;
                }
            }
            finally
            {
                BtnEntry.IsEnabled = BtnExit.IsEnabled = true;
            }
            return Task.CompletedTask;
        }
    }
}
