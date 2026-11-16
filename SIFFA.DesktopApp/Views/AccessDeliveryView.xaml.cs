using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using SIFFA.Core.Models.Custom.Response;
using SIFFA.Core.Services.AccessControlService;
using System.ComponentModel;
using System.Windows;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Boletos adicionales del empleado que acaba de ingresar, para marcar cuáles se entregaron.</summary>
    public partial class AccessDeliveryView : Window
    {
        private readonly IServiceProvider _services;
        private readonly AddTraceResponse _response;
        private readonly List<Row> _rows;

        public AccessDeliveryView(IServiceProvider services, AddTraceResponse response)
        {
            InitializeComponent();
            _services = services;
            _response = response;

            TxtName.Text = response.employeeName;
            TxtNumber.Text = $"No. {response.employeeNumber}";
            TxtMessage.Text = response.message;

            _rows = (response.employeeAdds ?? new List<AddEmployeeModel>())
                .Select(a => new Row
                {
                    AddTypeId = a.addTypeId,
                    Name = a.name,
                    Quantity = a.quantity,
                    AlreadyDelivered = a.deliveryAdd,
                    Delivered = a.deliveryAdd,
                })
                .ToList();
            Rows.ItemsSource = _rows;

            var any = _rows.Count > 0;
            RowsPanel.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            TxtEmpty.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
            BtnSave.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            BtnSkip.Content = any ? "Omitir" : "Cerrar";
            UpdateSave();
        }

        /// <summary>Cuántos boletos se marcaron como entregados en esta ventana.</summary>
        public int DeliveredCount { get; private set; }

        private sealed class Row : INotifyPropertyChanged
        {
            public Guid AddTypeId { get; init; }
            public string Name { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public bool AlreadyDelivered { get; init; }
            public bool CanEdit => !AlreadyDelivered;
            public bool Delivered { get; set; }
            public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        }

        private void OnToggle(object sender, RoutedEventArgs e) => UpdateSave();

        private void UpdateSave() =>
            BtnSave.IsEnabled = _rows.Any(r => r.Delivered && !r.AlreadyDelivered);

        private void OnSave(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            BtnSave.IsEnabled = false;
            try
            {
                var chosen = _rows.Where(r => r.Delivered && !r.AlreadyDelivered).ToList();
                using var scope = _services.CreateScope();
                var result = scope.ServiceProvider.GetRequiredService<IAccessControlService>().SaveDeliveries(
                    new SaveDeliveriesRequest
                    {
                        EmployeeNumber = _response.employeeNumber ?? string.Empty,
                        DeliveredAddTypeIds = chosen.Select(r => r.AddTypeId).ToList(),
                    });

                if (result.IsSuccess)
                {
                    DeliveredCount = chosen.Count;
                    DialogResult = true;
                    return;
                }
                TxtError.Text = result.Message;
                TxtError.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                TxtError.Text = $"No se pudieron guardar las entregas: {ex.GetBaseException().Message}";
                TxtError.Visibility = Visibility.Visible;
            }
            finally
            {
                UpdateSave();
            }
        }

        private void OnSkip(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
