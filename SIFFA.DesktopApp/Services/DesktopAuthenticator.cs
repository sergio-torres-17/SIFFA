using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Services.AuthService;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>
    /// Valida el login según el rol del equipo:
    /// servidor y cliente de otro escritorio, contra Security.Credential en SQL Server; cliente de consola web, contra su API.
    /// </summary>
    public class DesktopAuthenticator
    {
        private readonly IServiceProvider _services;
        private readonly MachineSettingsStore _settingsStore;

        public DesktopAuthenticator(IServiceProvider services, MachineSettingsStore settingsStore)
        {
            _services = services;
            _settingsStore = settingsStore;
        }

        public async Task<AuthResult> LoginAsync(string? username, string? password)
        {
            var settings = _settingsStore.Load();

            try
            {
                if (settings.Role == MachineRole.Client && settings.ClientTarget == ClientTarget.WebConsole)
                    return await LoginWebAsync(settings.WebConsoleUrl!, username, password);

                // Un ámbito nuevo en cada intento: así toma siempre la conexión vigente del equipo.
                using var scope = _services.CreateScope();
                return await scope.ServiceProvider.GetRequiredService<IAuthService>().ValidateAsync(username, password);
            }
            catch (Exception ex)
            {
                return new AuthResult(false, $"No se pudo validar el acceso: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static async Task<AuthResult> LoginWebAsync(string url, string? username, string? password)
        {
            using var http = new HttpClient { BaseAddress = ConnectionTester.Normalize(url), Timeout = TimeSpan.FromSeconds(10) };
            using var response = await http.PostAsJsonAsync("api/Auth/Login", new { username, password });

            // La API responde 200 con el usuario, o 401 con el mensaje de error.
            var result = await response.Content.ReadFromJsonAsync<AuthResult>(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            return result ?? new AuthResult(false, $"Respuesta inesperada de la consola web ({(int)response.StatusCode}).");
        }
    }
}
