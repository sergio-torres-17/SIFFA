using System.Windows;
using SIFFA.DesktopApp.Services;

namespace SIFFA.DesktopApp.Views
{
    public enum ShellOutcome { Exit, Logout, Reconfigure }

    /// <summary>Ventana principal (provisional): confirma que el equipo y la sesión están listos.</summary>
    public partial class ShellView : Window
    {
        public ShellView(UserSession session, MachineSettings settings)
        {
            InitializeComponent();
            TxtUser.Text = session.Name;
            TxtWelcome.Text = $"Bienvenido, {session.Name}";
            TxtMode.Text = settings.Describe();
        }

        public ShellOutcome Outcome { get; private set; } = ShellOutcome.Exit;

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            Outcome = ShellOutcome.Logout;
            Close();
        }

        private void OnReconfigure(object sender, RoutedEventArgs e)
        {
            Outcome = ShellOutcome.Reconfigure;
            Close();
        }
    }
}
