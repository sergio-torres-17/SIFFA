using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.AddTypeService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Catálogo de adicionales: alta, edición y baja.</summary>
    public partial class AddsPage : UserControl
    {
        private readonly IServiceProvider _services;

        public AddsPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            Loaded += async (_, _) => await LoadAsync();
        }

        private sealed class Row
        {
            public required AddTypeItem Item { get; init; }
            public bool CanDelete => Item.TicketsAssigned == 0 && Item.Configurations == 0;
            public string DeleteTip => CanDelete ? "Eliminar" : "En uso: no se puede eliminar";
            public string Usage => $"{Item.TicketsAssigned} boletos asignados · {Item.Configurations} configuraciones";
        }

        private async Task LoadAsync()
        {
            try
            {
                using var scope = _services.CreateScope();
                var items = await scope.ServiceProvider.GetRequiredService<IAddTypeService>().GetAllAsync();
                List.ItemsSource = items.Select(i => new Row { Item = i }).ToList();
                TxtEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudieron cargar los adicionales: {ex.GetBaseException().Message}", ok: false);
            }
        }

        private void ShowStatus(string message, bool ok)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = Visibility.Visible;
        }

        private async void OnCreate(object sender, RoutedEventArgs e)
        {
            if (new AddTypeEditView(_services) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
            {
                ShowStatus("Adicional creado.", ok: true);
                await LoadAsync();
            }
        }

        private async void OnEdit(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Row row) return;
            if (new AddTypeEditView(_services, row.Item) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
            {
                ShowStatus("Adicional actualizado.", ok: true);
                await LoadAsync();
            }
        }

        private async void OnDelete(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Row row) return;

            var answer = MessageBox.Show(Window.GetWindow(this),
                $"¿Eliminar \"{row.Item.Name}\"? Esta acción no se puede deshacer.",
                "Eliminar adicional", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                using var scope = _services.CreateScope();
                var result = await scope.ServiceProvider.GetRequiredService<IAddTypeService>().DeleteAsync(row.Item.AddTypeId);
                ShowStatus(result.Message, result.IsSuccess);
                if (result.IsSuccess) await LoadAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo eliminar: {ex.GetBaseException().Message}", ok: false);
            }
        }
    }
}
