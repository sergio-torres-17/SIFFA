using System.Windows;
using System.Windows.Media;
using SIFFA.DesktopApp.Services;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Primera ventana de configuración: servidor o cliente y, si es cliente, a qué se conecta.</summary>
    public partial class RoleSetupView : Window
    {
        public RoleSetupView(MachineSettings settings)
        {
            InitializeComponent();
            Settings = settings;

            // Se carga el estado guardado (al reconfigurar, la ventana aparece como se dejó).
            RbServer.IsChecked = settings.Role == MachineRole.Server;
            RbClient.IsChecked = settings.Role == MachineRole.Client;
            RbDesktop.IsChecked = settings.ClientTarget == ClientTarget.Desktop;
            RbWeb.IsChecked = settings.ClientTarget == ClientTarget.WebConsole;
            TxtAddress.Text = settings.ClientTarget == ClientTarget.WebConsole ? settings.WebConsoleUrl : settings.DbServer;
            UpdateUi();
        }

        /// <summary>La configuración que se está editando; al confirmar queda actualizada.</summary>
        public MachineSettings Settings { get; }

        private bool IsClient => RbClient.IsChecked == true;
        private bool IsWeb => IsClient && RbWeb.IsChecked == true;
        private bool IsDesktopClient => IsClient && RbDesktop.IsChecked == true;

        private void OnChoiceChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded && TxtAddress is null) return;
            UpdateUi();
        }

        private void OnAddressChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ShowStatus(null);

        private void UpdateUi()
        {
            if (ClientPanel is null) return;

            ClientPanel.Visibility = IsClient ? Visibility.Visible : Visibility.Collapsed;
            BtnTest.Visibility = IsWeb ? Visibility.Visible : Visibility.Collapsed;

            if (IsWeb)
            {
                AddressLabel.Text = "DIRECCIÓN DE LA CONSOLA WEB";
                AddressHint.Text = "Por ejemplo: https://siffa.miempresa.com";
                BtnNext.Content = "Guardar y continuar";
            }
            else
            {
                AddressLabel.Text = "DIRECCIÓN DEL ESCRITORIO SERVIDOR";
                AddressHint.Text = "Nombre o IP del equipo, con instancia o puerto si aplica. Por ejemplo: 192.168.1.20  o  EQUIPO\\SQLEXPRESS  o  192.168.1.20,1433";
                BtnNext.Content = "Siguiente";
            }

            ShowStatus(null);
        }

        private void ShowStatus(string? message, bool ok = false)
        {
            if (TxtStatus is null) return;
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        // Devuelve el mensaje de error si algo falta o es inválido.
        private string? Validate()
        {
            if (RbServer.IsChecked != true && !IsClient)
                return "Elige si este equipo es Servidor o Cliente.";

            if (!IsClient)
                return null;

            if (!IsWeb && !IsDesktopClient)
                return "Elige a qué se conecta el cliente.";

            var address = TxtAddress.Text.Trim();
            if (address.Length == 0)
                return IsWeb ? "Escribe la dirección de la consola web." : "Escribe la dirección del escritorio servidor.";

            if (IsWeb && !(Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
                return "La dirección debe empezar con http:// o https://";

            return null;
        }

        private async void OnTest(object sender, RoutedEventArgs e)
        {
            var error = Validate();
            if (error is not null) { ShowStatus(error); return; }

            BtnTest.IsEnabled = false;
            ShowStatus("Probando conexión…", ok: true);
            var (ok, message) = await ConnectionTester.TestWebConsoleAsync(TxtAddress.Text);
            ShowStatus(message, ok);
            BtnTest.IsEnabled = true;
        }

        private void OnNext(object sender, RoutedEventArgs e)
        {
            var error = Validate();
            if (error is not null) { ShowStatus(error); return; }

            if (RbServer.IsChecked == true)
            {
                Settings.Role = MachineRole.Server;
                Settings.ClientTarget = null;
                Settings.WebConsoleUrl = null;
            }
            else
            {
                Settings.Role = MachineRole.Client;
                Settings.ClientTarget = IsWeb ? ClientTarget.WebConsole : ClientTarget.Desktop;
                if (IsWeb)
                    Settings.WebConsoleUrl = TxtAddress.Text.Trim();
                else
                    Settings.DbServer = TxtAddress.Text.Trim();
            }

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
