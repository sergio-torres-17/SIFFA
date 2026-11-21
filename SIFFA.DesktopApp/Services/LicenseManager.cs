using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using SIFFA.Core.Services.AppregistryService;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>
    /// Licencia de este equipo contra Appregistry. La dirección, el nombre de la app y la API-KEY salen de la sección
    /// "Appregistry" de la configuración; la clave de licencia la escribe el usuario una vez y se guarda en
    /// %LOCALAPPDATA%\SIFFA\license.key (sobrevive a las actualizaciones).
    /// </summary>
    public class LicenseManager
    {
        private static readonly string Folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIFFA");
        private static readonly string KeyFile = Path.Combine(Folder, "license.key");
        private static readonly string StateFile = Path.Combine(Folder, "license.state");

        private readonly IConfiguration _config;

        public LicenseManager(IConfiguration config) => _config = config;

        /// <summary>Si la sección Appregistry no trae Url, no se valida nada.</summary>
        public bool IsEnabled => !string.IsNullOrWhiteSpace(_config["Appregistry:Url"]);

        public string? SavedKey
        {
            get { try { return File.Exists(KeyFile) ? File.ReadAllText(KeyFile).Trim() : null; } catch { return null; } }
        }

        public void SaveKey(string key)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(KeyFile, key.Trim().ToUpperInvariant());
        }

        /// <summary>Valida con la clave dada (o la guardada). Si queda válida, la guarda.</summary>
        public async Task<LicenseResult> ValidateAsync(string? key = null)
        {
            var options = new AppregistryOptions
            {
                Url = _config["Appregistry:Url"],
                ApiKey = _config["Appregistry:ApiKey"],
                AppName = _config["Appregistry:AppName"],
                GraceDays = int.TryParse(_config["Appregistry:GraceDays"], out var g) && g >= 0 ? g : 7,
                LicenseKey = key ?? SavedKey,
                MachineId = MachineId(),
            };
            var result = await AppregistryLicense.ValidateAsync(options, StateFile);
            if (key is not null && result.State == LicenseState.Valid)
                SaveKey(key);
            return result;
        }

        // Identificador estable del equipo (GUID de Windows, solo se envía su hash).
        private static string MachineId()
        {
            string? guid = null;
            try
            {
                using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                guid = k?.GetValue("MachineGuid") as string;
            }
            catch { }
            return AppregistryLicense.HashMachineId(string.IsNullOrEmpty(guid) ? Environment.MachineName : guid);
        }
    }
}
