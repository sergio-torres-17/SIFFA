using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SIFFA.DesktopApp.Services.Web
{
    /// <summary>Error al hablar con la consola web (sin conexión, sesión vencida, respuesta inesperada…).</summary>
    public class WebApiException : Exception
    {
        public WebApiException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Cliente de la API del escritorio (api/desktop/...) de la consola web. Toma la dirección de la configuración del
    /// equipo y el token de la sesión iniciada en cada llamada, así que reconfigurar o volver a entrar no exige reiniciar.
    /// Hay versión síncrona porque varios servicios del Core (registro de accesos, búsqueda) lo son.
    /// </summary>
    public class WebApi
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
        // Sin redirecciones: una redirección al login significa que la sesión venció, no una respuesta válida.
        private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(60) };

        private readonly MachineSettingsStore _settings;
        private readonly SessionStore _session;

        public WebApi(MachineSettingsStore settings, SessionStore session)
        {
            _settings = settings;
            _session = session;
        }

        // ---------- síncrono ----------

        public T Get<T>(string path) where T : class => Send<T>(HttpMethod.Get, path, null)!;
        public T Post<T>(string path, object? body) where T : class => Send<T>(HttpMethod.Post, path, body)!;

        private T? Send<T>(HttpMethod method, string path, object? body) where T : class
        {
            using var request = Build(method, path, body);
            HttpResponseMessage response;
            try { response = Http.Send(request); }
            catch (Exception ex) { throw Offline(ex); }

            using (response)
            {
                Check(response);
                using var stream = response.Content.ReadAsStream();
                return JsonSerializer.Deserialize<T>(stream, Json) ?? throw Unexpected();
            }
        }

        // ---------- asíncrono ----------

        public Task<T> GetAsync<T>(string path) where T : class => SendAsync<T>(HttpMethod.Get, path, null)!;
        public Task<T?> GetOrNullAsync<T>(string path) where T : class => SendAsync<T>(HttpMethod.Get, path, null, allowNotFound: true);
        public Task<T> PostAsync<T>(string path, object? body) where T : class => SendAsync<T>(HttpMethod.Post, path, body)!;
        public Task<T> PutAsync<T>(string path, object? body) where T : class => SendAsync<T>(HttpMethod.Put, path, body)!;
        public Task<T> DeleteAsync<T>(string path) where T : class => SendAsync<T>(HttpMethod.Delete, path, null)!;

        private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, bool allowNotFound = false) where T : class
        {
            using var request = Build(method, path, body);
            HttpResponseMessage response;
            try { response = await Http.SendAsync(request).ConfigureAwait(false); }
            catch (Exception ex) { throw Offline(ex); }

            using (response)
            {
                if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound) return null;
                Check(response);
                return await response.Content.ReadFromJsonAsync<T>(Json).ConfigureAwait(false) ?? throw Unexpected();
            }
        }

        // ---------- comunes ----------

        private HttpRequestMessage Build(HttpMethod method, string path, object? body)
        {
            var url = _settings.Load().WebConsoleUrl;
            if (string.IsNullOrWhiteSpace(url))
                throw new WebApiException("Este equipo no tiene configurada la dirección de la consola web.");

            var request = new HttpRequestMessage(method, new Uri(ConnectionTester.Normalize(url), "api/desktop/" + path));
            var token = _session.Load()?.Token;
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
                request.Content = JsonContent.Create(body, options: Json);
            return request;
        }

        private static void Check(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden || (int)response.StatusCode is >= 300 and < 400)
                throw new WebApiException("La sesión con la consola web venció. Cierra sesión y vuelve a entrar.");
            throw new WebApiException($"La consola web respondió {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        private static WebApiException Offline(Exception ex) =>
            new($"No se pudo conectar con la consola web: {ex.GetBaseException().Message}", ex);

        private static WebApiException Unexpected() => new("La consola web devolvió una respuesta inesperada.");
    }
}
