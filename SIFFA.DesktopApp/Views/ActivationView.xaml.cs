using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Models.Custom.Request.ManagementPlattform;
using SIFFA.Core.Services.ManagementPlattformService;
using System.Windows;
using System.Windows.Media;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Configuración inicial del sistema: crea la contraseña del usuario administrador.</summary>
    public partial class ActivationView : Window
    {
        private readonly IServiceProvider _services;

        public ActivationView(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            Loaded += (_, _) => TxtKey.Focus();
        }

        private void OnChanged(object sender, RoutedEventArgs e) => ShowStatus(null);

        private void ShowStatus(string? message, bool ok = false)
        {
            if (TxtStatus is null) return;
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void OnActivate(object sender, RoutedEventArgs e)
        {
            if (TxtKey.Password.Length == 0) { ShowStatus("Escribe la clave de activación."); return; }
            if (TxtPassword.Password.Length < 8) { ShowStatus("La contraseña debe tener al menos 8 caracteres."); return; }
            if (TxtPassword.Password != TxtConfirm.Password) { ShowStatus("Las contraseñas no coinciden."); return; }

            BtnActivate.IsEnabled = false;
            BtnActivate.Content = "Activando…";
            try
            {
                using var scope = _services.CreateScope();
                var platform = scope.ServiceProvider.GetRequiredService<IManagementPlattform>();
                var result = await platform.ActivateSystem(new ActivateSystemRequest
                {
                    SeedActivation = TxtKey.Password,
                    PasswordAdmin = TxtPassword.Password,
                });

                if (result.IsSuccess)
                {
                    MessageBox.Show(this, "Sistema activado. Ya puedes iniciar sesión con el usuario administrador.",
                        "SIFFA", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    return;
                }
                ShowStatus(result.Message);
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo activar: {ex.GetBaseException().Message}");
            }
            finally
            {
                BtnActivate.IsEnabled = true;
                BtnActivate.Content = "Activar";
            }
        }

        private void OnSkip(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
