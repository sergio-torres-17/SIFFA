using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SIFFA.DesktopApp.Services;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Segunda ventana de configuración: datos de conexión a SQL Server.</summary>
    public partial class DbConnectionView : Window
    {
        private bool _tested;

        public DbConnectionView(MachineSettings settings)
        {
            InitializeComponent();
            Settings = settings;

            TxtIntro.Text = settings.Role == MachineRole.Server
                ? "Datos del SQL Server que aloja la base de datos de SIFFA en este equipo."
                : "Datos del SQL Server del escritorio servidor al que se conectará este equipo.";

            TxtServer.Text = string.IsNullOrWhiteSpace(settings.DbServer) ? "localhost" : settings.DbServer;
            TxtDatabase.Text = string.IsNullOrWhiteSpace(settings.DbName) ? "SIFFA" : settings.DbName;
            RbWindows.IsChecked = settings.DbWindowsAuth;
            RbSql.IsChecked = !settings.DbWindowsAuth;
            TxtUser.Text = settings.DbUser ?? "";
            TxtPassword.Password = settings.DbPassword ?? "";
            ChkTrust.IsChecked = settings.DbTrustCertificate;
            // Automontar solo tiene sentido en el equipo servidor (el cliente se conecta a una base ya montada).
            ChkAutoMount.Visibility = settings.Role == MachineRole.Server ? Visibility.Visible : Visibility.Collapsed;
            ChkAutoMount.IsChecked = settings.DbAutoMount;
            ApplyAuthMode();
            _tested = false;
        }

        public MachineSettings Settings { get; }

        private void OnFieldChanged(object sender, RoutedEventArgs e)
        {
            if (TxtStatus is null) return;
            _tested = false;
            ShowStatus(null);
        }

        private void OnAuthChanged(object sender, RoutedEventArgs e)
        {
            if (SqlAuthPanel is null) return;
            ApplyAuthMode();
            OnFieldChanged(sender, e);
        }

        private void ApplyAuthMode() =>
            SqlAuthPanel.Visibility = RbSql.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        private void ShowStatus(string? message, bool ok = false)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        private string? Validate()
        {
            if (string.IsNullOrWhiteSpace(TxtServer.Text)) return "Escribe el servidor SQL.";
            if (string.IsNullOrWhiteSpace(TxtDatabase.Text)) return "Escribe el nombre de la base de datos.";
            if (RbSql.IsChecked == true && string.IsNullOrWhiteSpace(TxtUser.Text)) return "Escribe el usuario de SQL Server.";
            return null;
        }

        private void Apply()
        {
            Settings.DbServer = TxtServer.Text.Trim();
            Settings.DbName = TxtDatabase.Text.Trim();
            Settings.DbWindowsAuth = RbWindows.IsChecked == true;
            Settings.DbUser = Settings.DbWindowsAuth ? null : TxtUser.Text.Trim();
            Settings.DbPassword = Settings.DbWindowsAuth ? null : TxtPassword.Password;
            Settings.DbTrustCertificate = ChkTrust.IsChecked == true;
            Settings.DbAutoMount = Settings.Role == MachineRole.Server && ChkAutoMount.IsChecked == true;
        }

        private async Task<bool> RunTestAsync()
        {
            var error = Validate();
            if (error is not null) { ShowStatus(error); return false; }

            Apply();
            BtnTest.IsEnabled = BtnSave.IsEnabled = false;
            ShowStatus("Probando conexión…", ok: true);
            // Con "Automontar" marcado, primero se crea la base y su estructura si faltan.
            if (Settings.DbAutoMount)
            {
                var mount = await DatabaseProvisioner.ProvisionAsync(Settings, new Progress<string>(text => ShowStatus(text, ok: true)));
                if (!mount.Ok)
                {
                    ShowStatus(mount.Message, ok: false);
                    BtnTest.IsEnabled = BtnSave.IsEnabled = true;
                    _tested = false;
                    return false;
                }
            }
            var (ok, message) = await ConnectionTester.TestDatabaseAsync(Settings);
            ShowStatus(message, ok);
            BtnTest.IsEnabled = BtnSave.IsEnabled = true;
            _tested = ok;
            return ok;
        }

        private async void OnTest(object sender, RoutedEventArgs e) => await RunTestAsync();

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            var error = Validate();
            if (error is not null) { ShowStatus(error); return; }

            if (!_tested && !await RunTestAsync())
            {
                var answer = MessageBox.Show(this,
                    "No se pudo comprobar la conexión. ¿Guardar la configuración de todos modos?",
                    "SIFFA", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
            }

            Apply();
            DialogResult = true;
        }

        private void OnBack(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
