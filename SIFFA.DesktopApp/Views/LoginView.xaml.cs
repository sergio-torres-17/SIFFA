using System.Windows;
using SIFFA.DesktopApp.Services;

namespace SIFFA.DesktopApp.Views
{
    public enum LoginOutcome { Cancelled, SignedIn, Reconfigure }

    /// <summary>Inicio de sesión: valida contra la base local, el escritorio servidor o la consola web.</summary>
    public partial class LoginView : Window
    {
        private readonly DesktopAuthenticator _auth;
        private readonly SessionStore _sessions;

        public LoginView(DesktopAuthenticator auth, SessionStore sessions, MachineSettings settings)
        {
            InitializeComponent();
            _auth = auth;
            _sessions = sessions;
            TxtMode.Text = settings.Describe();
            Loaded += (_, _) => TxtUser.Focus();
        }

        public LoginOutcome Outcome { get; private set; } = LoginOutcome.Cancelled;

        private async void OnLogin(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(TxtUser.Text) || TxtPassword.Password.Length == 0)
            {
                ShowError("Escribe tu correo y contraseña.");
                return;
            }

            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Validando…";
            var result = await _auth.LoginAsync(TxtUser.Text, TxtPassword.Password);
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Entrar";

            if (!result.IsSuccess)
            {
                ShowError(result.Message);
                TxtPassword.Clear();
                TxtPassword.Focus();
                return;
            }

            _sessions.Save(result.UserId, result.UserName ?? TxtUser.Text.Trim(), result.Email ?? TxtUser.Text.Trim());
            Outcome = LoginOutcome.SignedIn;
            DialogResult = true;
        }

        private void OnReconfigure(object sender, RoutedEventArgs e)
        {
            Outcome = LoginOutcome.Reconfigure;
            DialogResult = false;
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
