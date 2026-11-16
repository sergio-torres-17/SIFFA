using Microsoft.Extensions.Configuration;
using System.IO;
using Velopack;
using Velopack.Sources;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>
    /// Busca, descarga y aplica actualizaciones con Velopack.
    /// Solo actúa cuando la app está instalada con Velopack; ejecutándola desde Visual Studio no hace nada.
    /// </summary>
    public class AppUpdater
    {
        private readonly IConfiguration _configuration;

        public AppUpdater(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Descarga la actualización si existe una más nueva. Devuelve su versión y un delegado que reinicia la app
        /// para aplicarla, o null si no hay nada que actualizar.
        /// </summary>
        public async Task<(string Version, Action ApplyAndRestart)?> CheckAndDownloadAsync()
        {
            var source = _configuration["Updates:Source"];
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var manager = new UpdateManager(CreateSource(source));
            if (!manager.IsInstalled)
                return null;

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
                return null;

            await manager.DownloadUpdatesAsync(update);
            return (update.TargetFullRelease.Version.ToString(), () => manager.ApplyUpdatesAndRestart(update));
        }

        // Una URL de GitHub se trata como repositorio de releases; cualquier otra ruta o URL como carpeta de releases.
        private IUpdateSource CreateSource(string source)
        {
            if (source.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                var prerelease = bool.TryParse(_configuration["Updates:Prerelease"], out var pre) && pre;
                var token = _configuration["Updates:AccessToken"];
                return new GithubSource(source, token, prerelease);
            }

            return Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
                ? new SimpleWebSource(source)
                : new SimpleFileSource(new DirectoryInfo(source));
        }
    }
}
