using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Models;
using SIFFA.Core.Services.AuthService;
using SIFFA.Core.Services.UserAccountService;
using SIFFA.Core.Services.AccessControlService;
using SIFFA.Core.Services.AddTypeService;
using SIFFA.Core.Services.DashboardService;
using SIFFA.Core.Services.EmployeeService;
using SIFFA.Core.Services.EncryptionService;
using SIFFA.Core.Services.ManagementPlattformService;
using SIFFA.DesktopApp.Services;
using SIFFA.DesktopApp.Views;
using System.IO;
using System.Windows;

namespace SIFFA.DesktopApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Configuration = BuildConfiguration();

            var services = new ServiceCollection();
            ConfigureServices(services);
            // ValidateOnBuild: si falta registrar alguna dependencia, falla al arrancar con un mensaje claro y no al usar la ventana.
            Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        /// <summary>Contenedor de dependencias de la aplicación. Los servicios con ámbito (scoped) se piden dentro de un <c>CreateScope()</c>.</summary>
        public IServiceProvider Services { get; }

        public IConfiguration Configuration { get; }

        public new static App Current => (App)Application.Current;

        // Orden de fuentes (la última gana):
        //   1) appsettings.json junto al ejecutable (valores por defecto; Velopack lo reemplaza en cada actualización).
        //   2) %LOCALAPPDATA%\SIFFA\appsettings.user.json: configuración propia de cada equipo (por ejemplo, su cadena
        //      de conexión local). Vive fuera de la carpeta de instalación, así que sobrevive a las actualizaciones.
        private static IConfiguration BuildConfiguration()
        {
            var userSettings = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIFFA", "appsettings.user.json");

            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile(userSettings, optional: true, reloadOnChange: false)
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            //Configuration (la necesitan, por ejemplo, ManagementPlattform y AppUpdater)
            services.AddSingleton(Configuration);

            //Configuración de este equipo (servidor/cliente), sesión y autenticación
            services.AddSingleton<MachineSettingsStore>();
            services.AddSingleton<SessionStore>();
            services.AddSingleton<DesktopAuthenticator>();

            //Configure database connection: se resuelve en cada uso, así que reconfigurar el equipo no exige reiniciar.
            //Primero la configuración del equipo; si no existe, ConnectionStrings:DefaultConnection de appsettings.
            services.AddDbContext<SiffaContext>((sp, conf) =>
                conf.UseSqlServer(sp.GetRequiredService<MachineSettingsStore>().ResolveConnectionString(Configuration)));

            //Configure services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IManagementPlattform, ManagementPlattform>();
            services.AddScoped<IAddTypeService, AddTypeService>();
            services.AddScoped<IAccessControlService, AccessControlService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<SIFFA.Core.Services.AttendanceService.IAttendanceService, SIFFA.Core.Services.AttendanceService.AttendanceService>();
            services.AddSingleton<AppUpdater>();

            //Configure windows
            services.AddTransient<SplashView>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await RunAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SIFFA no pudo iniciar:\n\n{ex.Message}", "SIFFA", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Shutdown();
            }
        }

        private async Task RunAsync()
        {
            var store = Services.GetRequiredService<MachineSettingsStore>();
            var sessions = Services.GetRequiredService<SessionStore>();

            // 1) Splash: revisa si el equipo ya está configurado y si hay sesión iniciada.
            var splash = Services.GetRequiredService<SplashView>();
            splash.Show();

            splash.SetStatus("Verificando configuración del equipo…");
            await Task.Delay(800);
            var settings = store.Load();

            UserSession? session = null;
            if (settings.IsConfigured)
            {
                splash.SetStatus("Verificando sesión…");
                await Task.Delay(800);
                session = sessions.Load();
                splash.SetStatus(session is null ? "Se requiere iniciar sesión" : $"Hola, {session.Name}");
            }
            else
            {
                splash.SetStatus("Equipo sin configurar");
            }
            await Task.Delay(500);
            splash.Close();

            // 2) Configuración → login → ventana principal, repitiendo según lo que pida el usuario.
            while (true)
            {
                if (!settings.IsConfigured)
                {
                    if (!RunSetup(settings))
                        return; // canceló la configuración: no se puede continuar
                    store.Save(settings);
                    session = null;
                }

                if (session is null)
                {
                    // Primer uso del sistema: si el administrador aún no tiene contraseña, se ofrece activarlo.
                    if (await NeedsActivationAsync(settings))
                        new ActivationView(Services).ShowDialog();

                    var login = new LoginView(Services.GetRequiredService<DesktopAuthenticator>(), sessions, settings);
                    login.ShowDialog();
                    if (login.Outcome == LoginOutcome.Reconfigure)
                    {
                        if (RunSetup(settings))
                        {
                            store.Save(settings);
                            sessions.Clear();
                        }
                        continue;
                    }
                    if (login.Outcome != LoginOutcome.SignedIn)
                        return;

                    session = sessions.Load();
                    if (session is null)
                        return;
                }

                var shell = new ShellView(session, settings);
                var updateCheck = CheckForUpdatesAsync();
                shell.ShowDialog();
                await updateCheck;

                switch (shell.Outcome)
                {
                    case ShellOutcome.Logout:
                        sessions.Clear();
                        session = null;
                        break;
                    case ShellOutcome.Reconfigure:
                        if (RunSetup(settings))
                        {
                            store.Save(settings);
                            sessions.Clear();
                            session = null;
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        // Solo aplica cuando este equipo habla directo con SQL Server. Si la base no responde, se omite:
        // el login mostrará el error de conexión.
        private async Task<bool> NeedsActivationAsync(MachineSettings settings)
        {
            if (!settings.UsesDatabase)
                return false;

            try
            {
                using var scope = Services.CreateScope();
                var platform = scope.ServiceProvider.GetRequiredService<IManagementPlattform>();
                return !await platform.IsSystemActivated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo verificar la activación: {ex.Message}");
                return false;
            }
        }

        // Ventana 1 (servidor/cliente) → ventana 2 (base de datos, solo si usa SQL Server). "Atrás" vuelve a la 1.
        private static bool RunSetup(MachineSettings settings)
        {
            while (true)
            {
                var role = new RoleSetupView(settings);
                if (role.ShowDialog() != true)
                    return false;

                if (!settings.UsesDatabase)
                    return true;

                var db = new DbConnectionView(settings);
                if (db.ShowDialog() == true)
                    return true;
            }
        }

        // La búsqueda de actualizaciones no debe impedir usar la app: sin internet o sin releases, simplemente se omite.
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var update = await Services.GetRequiredService<AppUpdater>().CheckAndDownloadAsync();
                if (update is null)
                    return;

                var answer = MessageBox.Show(
                    $"Hay una nueva versión de SIFFA ({update.Value.Version}) lista para instalar.\n\n¿Reiniciar ahora para actualizar?",
                    "Actualización disponible", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (answer == MessageBoxResult.Yes)
                    update.Value.ApplyAndRestart();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"No se pudo buscar actualizaciones: {ex.Message}");
            }
        }
    }
}
