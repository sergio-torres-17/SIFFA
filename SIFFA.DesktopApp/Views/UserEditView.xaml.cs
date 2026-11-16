using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.UserAccountService;
using System.Windows;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Alta de un usuario de acceso, o cambio de contraseña de uno existente.</summary>
    public partial class UserEditView : Window
    {
        private readonly IServiceProvider _services;
        private readonly UserAccountItem? _resetFor;

        public UserEditView(IServiceProvider services, UserAccountItem? resetFor = null)
        {
            InitializeComponent();
            _services = services;
            _resetFor = resetFor;

            if (resetFor is null)
            {
                TxtTitle.Text = "Nuevo usuario";
                TxtSubtitle.Text = "Podrá iniciar sesión en SIFFA con su correo y esta contraseña.";
            }
            else
            {
                TxtTitle.Text = "Cambiar contraseña";
                TxtSubtitle.Text = $"{resetFor.FullName} · {resetFor.Email}";
                DataPanel.Visibility = Visibility.Collapsed;
                LblPassword.Text = "NUEVA CONTRASEÑA";
            }

            Loaded += (_, _) => (resetFor is null ? (UIElement)TxtFirst : TxtPassword).Focus();
        }

        private void OnChanged(object sender, RoutedEventArgs e)
        {
            if (TxtStatus is not null) TxtStatus.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            TxtStatus.Text = message;
            TxtStatus.Visibility = Visibility.Visible;
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (TxtPassword.Password != TxtConfirm.Password)
            {
                ShowError("Las contraseñas no coinciden.");
                return;
            }

            BtnSave.IsEnabled = false;
            try
            {
                using var scope = _services.CreateScope();
                var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
                var result = _resetFor is null
                    ? await accounts.CreateAsync(TxtFirst.Text, TxtLast.Text, TxtEmail.Text, TxtPassword.Password)
                    : await accounts.ResetPasswordAsync(_resetFor.UserId, TxtPassword.Password);

                if (result.IsSuccess)
                    DialogResult = true;
                else
                    ShowError(result.Message);
            }
            catch (Exception ex)
            {
                ShowError($"No se pudo guardar: {ex.GetBaseException().Message}");
            }
            finally
            {
                BtnSave.IsEnabled = true;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
