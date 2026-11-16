using System.IO;
using System.Text.Json;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>
    /// Preferencias de "Reimprimir invitación" en este equipo: logo de la empresa (se elige una sola vez),
    /// tipo de código y orientación. Se guardan en %LOCALAPPDATA%\SIFFA.
    /// </summary>
    public class ReprintSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string SettingsPath => Path.Combine(MachineSettingsStore.Folder, "reprint.json");
        private static string LogoPath => Path.Combine(MachineSettingsStore.Folder, "invitation-logo.bin");

        /// <summary>true = código de barras, false = QR (por defecto barras, como las invitaciones impresas).</summary>
        public bool Barcode { get; set; } = true;

        /// <summary>true = el boleto se gira 90° para que su lado largo avance con el papel.</summary>
        public bool Vertical { get; set; } = true;

        /// <summary>Nombre del archivo del logo, solo para mostrarlo.</summary>
        public string? LogoName { get; set; }

        public static ReprintSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                    return JsonSerializer.Deserialize<ReprintSettings>(File.ReadAllText(SettingsPath)) ?? new ReprintSettings();
            }
            catch
            {
                // Archivo dañado: se vuelve a los valores por defecto.
            }
            return new ReprintSettings();
        }

        public void Save()
        {
            Directory.CreateDirectory(MachineSettingsStore.Folder);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }

        public static byte[]? LoadLogo()
        {
            try { return File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null; }
            catch { return null; }
        }

        public void SaveLogo(byte[] bytes, string name)
        {
            Directory.CreateDirectory(MachineSettingsStore.Folder);
            File.WriteAllBytes(LogoPath, bytes);
            LogoName = name;
            Save();
        }
    }
}
