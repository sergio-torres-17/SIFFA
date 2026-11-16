using SIFFA.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.UserAccountService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Lista, alta, cambio de contraseña y baja de los usuarios de acceso.</summary>
    public partial class UsersPage : UserControl
    {
        private readonly IServiceProvider _services;
        private readonly Guid? _currentUserId;

        public UsersPage(IServiceProvider services, Guid? currentUserId)
        {
            InitializeComponent();
            _services = services;
            _currentUserId = currentUserId;
            Loaded += async (_, _) => await LoadAsync();
        }

        // Fila de la lista: el usuario más lo que la vista necesita para pintarlo.
        private sealed class Row
        {
            public required UserAccountItem Item { get; init; }
            public bool CanDelete { get; init; }
            public Visibility AdminVisibility => Item.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            public string Status => Item.HasCredential ? "Con acceso" : "Sin contraseña";
            public Brush StatusBrush { get; init; } = Brushes.White;
        }

        private async Task LoadAsync()
        {
            try
            {
                using var scope = _services.CreateScope();
                var items = await scope.ServiceProvider.GetRequiredService<IUserAccountService>().GetAllAsync();

                var ok = (Brush)FindResource("BrushSuccess");
                var warn = (Brush)FindResource("BrushError");
                List.ItemsSource = items.Select(u => new Row
                {
                    Item = u,
                    CanDelete = !u.IsAdmin && u.UserId != _currentUserId,
                    StatusBrush = u.HasCredential ? ok : warn,
                }).ToList();
                TxtEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudieron cargar los usuarios: {ex.GetBaseException().Message}", ok: false);
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
            if (new UserEditView(_services) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
            {
                ShowStatus("Usuario creado.", ok: true);
                await LoadAsync();
            }
        }

        private async void OnResetPassword(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Row row) return;
            if (new UserEditView(_services, row.Item) { Owner = Window.GetWindow(this) }.ShowDialog() == true)
            {
                ShowStatus($"Contraseña de {row.Item.Email} actualizada.", ok: true);
                await LoadAsync();
            }
        }

        private async void OnDelete(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is not Row row) return;

            var answer = MessageBox.Show(Window.GetWindow(this),
                $"¿Eliminar a {row.Item.FullName} ({row.Item.Email})?\nYa no podrá iniciar sesión. Esta acción no se puede deshacer.",
                "Eliminar usuario", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                using var scope = _services.CreateScope();
                var result = await scope.ServiceProvider.GetRequiredService<IUserAccountService>()
                    .DeleteAsync(row.Item.UserId, _currentUserId);
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
