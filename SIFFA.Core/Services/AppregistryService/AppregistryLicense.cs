using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIFFA.Core.Services.AppregistryService
{
    /// <summary>Datos para validar la licencia de SIFFA contra Appregistry.</summary>
    public class AppregistryOptions
    {
        /// <summary>Dirección de Appregistry (p. ej. https://appregistry.multicodedev.net). Vacía = sin validación.</summary>
        public string? Url { get; set; }
        /// <summary>API-KEY de SIFFA en Appregistry (cabecera X-API-KEY).</summary>
        public string? ApiKey { get; set; }
        /// <summary>Nombre de la aplicación tal como está dado de alta en Appregistry.</summary>
        public string? AppName { get; set; }
        /// <summary>Clave de licencia de este equipo.</summary>
        public string? LicenseKey { get; set; }
        /// <summary>Identificador estable de este equipo; la licencia queda ligada al primero que la valida.</summary>
        public string? MachineId { get; set; }
        /// <summary>Días que se puede seguir usando si Appregistry no responde, contados desde la última validación correcta.</summary>
        public int GraceDays { get; set; } = 7;

        /// <summary>Hay datos suficientes para validar. Sin URL, la validación está desactivada.</summary>
        public bool IsEnabled => !string.IsNullOrWhiteSpace(Url);
    }

    public enum LicenseState
    {
        /// <summary>Appregistry no está configurado: no se valida nada.</summary>
        Disabled,
        /// <summary>Licencia válida (Appregistry respondió que sí).</summary>
        Valid,
        /// <summary>Appregistry no respondió, pero la última validación correcta cae dentro del periodo de gracia.</summary>
        Grace,
        /// <summary>No hay licencia válida: falta la clave, fue rechazada o venció el periodo de gracia.</summary>
        Invalid,
    }

    public record LicenseResult(LicenseState State, string Message, string? Holder = null, DateTime? ExpiresAt = null)
    {
        /// <summary>La app puede usarse.</summary>
        public bool CanUse => State is LicenseState.Disabled or LicenseState.Valid or LicenseState.Grace;
    }

    /// <summary>
    /// Valida la licencia contra Appregistry (POST /api/license/validate). Recuerda la última validación correcta en
    /// <c>stateFile</c> para dar un periodo de gracia si no hay internet o Appregistry está caído. Un rechazo explícito
    /// (licencia inexistente, desactivada, vencida o de otro equipo) nunca usa la gracia.
    /// </summary>
    public static class AppregistryLicense
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private sealed class ServerReply
        {
            public bool Valid { get; set; }
            public string? Error { get; set; }
            public string? Holder { get; set; }
            public DateTime? ExpiresAt { get; set; }
        }

        private sealed class SavedState
        {
            public string LicenseKey { get; set; } = "";
            public string MachineId { get; set; } = "";
            public DateTime LastValidUtc { get; set; }
            public string? Holder { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public string Signature { get; set; } = "";
        }

        public static async Task<LicenseResult> ValidateAsync(AppregistryOptions options, string stateFile, CancellationToken cancellationToken = default)
        {
            if (!options.IsEnabled)
                return new LicenseResult(LicenseState.Disabled, "La validación de licencia no está configurada.");
            if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.AppName))
                return new LicenseResult(LicenseState.Invalid, "Falta configurar la API-KEY o el nombre de la aplicación de Appregistry.");

            var key = (options.LicenseKey ?? "").Trim().ToUpperInvariant();
            var machine = (options.MachineId ?? "").Trim();
            if (key.Length == 0)
                return new LicenseResult(LicenseState.Invalid, "Falta la clave de licencia.");
            if (machine.Length == 0)
                return new LicenseResult(LicenseState.Invalid, "No se pudo determinar el identificador de este equipo.");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Base(options.Url!), "api/license/validate"))
                {
                    Content = JsonContent.Create(new { AppName = options.AppName, licenseKey = key, machineId = machine }),
                };
                request.Headers.Add("X-API-KEY", options.ApiKey);

                using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);

                // Errores del servidor (5xx) o proxys: cuentan como "no se pudo validar", no como rechazo.
                if ((int)response.StatusCode >= 500)
                    return Offline(options, key, machine, stateFile, $"Appregistry respondió {(int)response.StatusCode}.");

                ServerReply? reply = null;
                try { reply = await response.Content.ReadFromJsonAsync<ServerReply>(Json, cancellationToken).ConfigureAwait(false); }
                catch (JsonException) { /* respuesta que no es JSON (p. ej. la página de otro servidor) */ }

                if (response.StatusCode == HttpStatusCode.OK && reply?.Valid == true)
                {
                    Save(stateFile, options, key, machine, reply);
                    return new LicenseResult(LicenseState.Valid, "Licencia válida.", reply.Holder, reply.ExpiresAt);
                }

                // Rechazo explícito: se borra lo recordado para que la gracia no lo rescate.
                TryDelete(stateFile);
                var why = reply?.Error;
                if (string.IsNullOrWhiteSpace(why))
                    why = response.StatusCode == HttpStatusCode.Unauthorized
                        ? "La API-KEY no es válida."
                        : $"Appregistry rechazó la licencia ({(int)response.StatusCode}).";
                return new LicenseResult(LicenseState.Invalid, why);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
            {
                if (cancellationToken.IsCancellationRequested) throw;
                return Offline(options, key, machine, stateFile, $"No se pudo contactar a Appregistry ({ex.GetBaseException().Message}).");
            }
        }

        // Sin respuesta de Appregistry: se permite el uso si la última validación correcta (misma clave y equipo) es reciente.
        private static LicenseResult Offline(AppregistryOptions options, string key, string machine, string stateFile, string reason)
        {
            var saved = Load(stateFile, options);
            if (saved is not null && saved.LicenseKey == key && saved.MachineId == machine
                && (saved.ExpiresAt is null || saved.ExpiresAt > DateTime.Now))
            {
                var left = saved.LastValidUtc.AddDays(options.GraceDays) - DateTime.UtcNow;
                if (left > TimeSpan.Zero)
                    return new LicenseResult(LicenseState.Grace,
                        $"{reason} Se usa la última validación correcta; quedan {Math.Max(1, (int)Math.Ceiling(left.TotalDays))} día(s) de gracia.",
                        saved.Holder, saved.ExpiresAt);
            }
            return new LicenseResult(LicenseState.Invalid,
                $"{reason} No hay una validación reciente que permita continuar: conéctate a internet y vuelve a intentar.");
        }

        // ---------- estado recordado (firmado con la API-KEY para que no se edite a mano) ----------

        private static void Save(string file, AppregistryOptions o, string key, string machine, ServerReply reply)
        {
            try
            {
                var state = new SavedState
                {
                    LicenseKey = key, MachineId = machine, LastValidUtc = DateTime.UtcNow,
                    Holder = reply.Holder, ExpiresAt = reply.ExpiresAt,
                };
                state.Signature = Sign(o, state);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
                File.WriteAllText(file, JsonSerializer.Serialize(state, Json));
            }
            catch { /* no poder guardar solo quita la gracia; no debe romper el arranque */ }
        }

        private static SavedState? Load(string file, AppregistryOptions o)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var state = JsonSerializer.Deserialize<SavedState>(File.ReadAllText(file), Json);
                return state is not null && CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(state.Signature), Encoding.UTF8.GetBytes(Sign(o, state))) ? state : null;
            }
            catch { return null; }
        }

        private static string Sign(AppregistryOptions o, SavedState s)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(o.ApiKey ?? ""));
            var data = $"{s.LicenseKey}|{s.MachineId}|{s.LastValidUtc:O}|{s.Holder}|{s.ExpiresAt:O}";
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }

        private static void TryDelete(string file) { try { File.Delete(file); } catch { } }

        private static Uri Base(string url)
        {
            url = url.Trim();
            return new Uri(url.EndsWith('/') ? url : url + "/", UriKind.Absolute);
        }

        /// <summary>Identificador estable a partir de un valor propio del equipo (se guarda solo el hash).</summary>
        public static string HashMachineId(string raw) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("SIFFA|" + raw)))[..32].ToLowerInvariant();
    }
}
