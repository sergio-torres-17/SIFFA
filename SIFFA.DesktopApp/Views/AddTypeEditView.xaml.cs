using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.AddTypeService;
using System.Windows;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Alta o edición de un tipo de adicional (boleto de agua, comida, bebida…).</summary>
    public partial class AddTypeEditView : Window
    {
        private readonly IServiceProvider _services;
        private readonly AddTypeItem? _item;

        public AddTypeEditView(IServiceProvider services, AddTypeItem? item = null)
        {
            InitializeComponent();
            _services = services;
            _item = item;

            TxtTitle.Text = item is null ? "Nuevo adicional" : "Editar adicional";
            TxtName.Text = item?.Name ?? "";
            TxtDescription.Text = item?.Description ?? "";
            Loaded += (_, _) => { TxtName.Focus(); TxtName.SelectAll(); };
        }

        private void OnChanged(object sender, RoutedEventArgs e)
        {
            if (TxtStatus is not null) TxtStatus.Visibility = Visibility.Collapsed;
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                Show("Escribe el nombre del adicional.");
                return;
            }

            BtnSave.IsEnabled = false;
            try
            {
                using var scope = _services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAddTypeService>();
                var description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text;
                var result = _item is null
                    ? await service.CreateAsync(TxtName.Text, description)
                    : await service.UpdateAsync(_item.AddTypeId, TxtName.Text, description);

                if (result.IsSuccess) DialogResult = true;
                else Show(result.Message);
            }
            catch (Exception ex)
            {
                Show($"No se pudo guardar: {ex.GetBaseException().Message}");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void Show(string message)
        {
            TxtStatus.Text = message;
            TxtStatus.Visibility = Visibility.Visible;
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
