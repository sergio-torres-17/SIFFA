using System.Net.Http;
using Microsoft.Data.SqlClient;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>Comprueba que las conexiones configuradas funcionan antes de guardarlas.</summary>
    public static class ConnectionTester
    {
        public static async Task<(bool Ok, string Message)> TestDatabaseAsync(MachineSettings settings)
        {
            try
            {
                await using var connection = new SqlConnection(settings.BuildConnectionString());
                await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT OBJECT_ID('Security.Credential')";
                var hasSchema = await command.ExecuteScalarAsync() is not (null or DBNull);

                return hasSchema
                    ? (true, "Conexión correcta: la base de datos SIFFA está lista.")
                    : (true, "Conexión correcta, pero la base no tiene las tablas de SIFFA. Usa el botón \"Montar base de datos\" (equipo servidor) antes de iniciar sesión.");
            }
            catch (Exception ex)
            {
                return (false, $"No se pudo conectar: {ex.Message}");
            }
        }

        public static async Task<(bool Ok, string Message)> TestWebConsoleAsync(string url)
        {
            try
            {
                using var http = new HttpClient { BaseAddress = Normalize(url), Timeout = TimeSpan.FromSeconds(8) };
                using var response = await http.GetAsync("api/Auth/Ping");
                return response.IsSuccessStatusCode
                    ? (true, "Conexión correcta: la consola web respondió.")
                    : (false, $"La dirección respondió {(int)response.StatusCode}; no parece una consola SIFFA.");
            }
            catch (Exception ex)
            {
                return (false, $"No se pudo conectar: {ex.Message}");
            }
        }

        public static Uri Normalize(string url)
        {
            url = url.Trim();
            if (!url.EndsWith('/'))
                url += "/";
            return new Uri(url, UriKind.Absolute);
        }
    }
}
