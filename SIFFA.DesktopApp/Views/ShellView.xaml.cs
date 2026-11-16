using SIFFA.DesktopApp.Services;
using SIFFA.DesktopApp.Views.Pages;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SIFFA.DesktopApp.Views
{
    public enum ShellOutcome { Exit, Logout, Reconfigure }

    /// <summary>Ventana principal: menú lateral con las mismas secciones que la web.</summary>
    public partial class ShellView : Window
    {
        private readonly UserSession _session;
        private readonly MachineSettings _settings;

        public ShellView(UserSession session, MachineSettings settings)
        {
            InitializeComponent();
            _session = session;
            _settings = settings;

            TxtUser.Text = session.Name;
            TxtMode.Text = settings.Describe();
            Loaded += (_, _) => NavHome.IsChecked = true;
        }

        public ShellOutcome Outcome { get; private set; } = ShellOutcome.Exit;

        private void OnNav(object sender, RoutedEventArgs e)
        {
            if (Host is null) return;
            var tag = (string)((RadioButton)sender).Tag;

            // Con consola web el equipo no tiene base de datos propia: las secciones se usan desde la web.
            if (!_settings.UsesDatabase)
            {
                Host.Content = BuildWebOnlyNotice();
                return;
            }

            var services = App.Current.Services;
            Host.Content = tag switch
            {
                "home" => new HomePage(services),
                "access" => new AccessPage(services),
                "cards" => new CardsPage(services),
                "attendance" => new AttendancePage(services),
                "upload" => new UploadPage(services),
                "adds" => new AddsPage(services),
                "users" => new UsersPage(services, _session.UserId),
                _ => null,
            };
        }

        private UIElement BuildWebOnlyNotice()
        {
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 460 };
            panel.Children.Add(new TextBlock { Text = "Este equipo usa la consola web", Style = (Style)FindResource("Heading"), TextAlignment = TextAlignment.Center });
            panel.Children.Add(new TextBlock
            {
                Text = "Los datos viven en el servidor web. Administra empleados, invitaciones, accesos y usuarios desde la consola.",
                Style = (Style)FindResource("Muted"), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 18),
            });
            var open = new Button { Content = "Abrir consola web", Style = (Style)FindResource("PrimaryButton"), HorizontalAlignment = HorizontalAlignment.Center };
            open.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(_settings.WebConsoleUrl))
                    Process.Start(new ProcessStartInfo(_settings.WebConsoleUrl) { UseShellExecute = true });
            };
            panel.Children.Add(open);
            return panel;
        }

        // Busca a un empleado y reimprime su invitación en la impresora térmica.
        private void OnReprint(object sender, RoutedEventArgs e)
        {
            // Con consola web el equipo no tiene base de datos propia: la reimpresión se hace desde la web.
            if (!_settings.UsesDatabase)
            {
                MessageBox.Show(this, "Este equipo usa la consola web y no tiene los empleados guardados aquí. La reimpresión de invitaciones se hace en un equipo con base de datos (servidor o cliente de escritorio).",
                    "Reimprimir invitación", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new ReprintInvitationView(App.Current.Services) { Owner = this }.ShowDialog();
        }

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
