using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SIFFA.DesktopApp.Services
{
    public enum MachineRole
    {
        Server = 1,
        Client = 2,
    }

    /// <summary>A qué se conecta un equipo cliente.</summary>
    public enum ClientTarget
    {
        /// <summary>Otro escritorio SIFFA: se usa directamente su SQL Server.</summary>
        Desktop = 1,
        /// <summary>La consola web SIFFA: el login se valida contra su API.</summary>
        WebConsole = 2,
    }

    /// <summary>Configuración de este equipo (rol y conexión). Se guarda en %LOCALAPPDATA%\SIFFA\machine.json.</summary>
    public class MachineSettings
    {
        public MachineRole? Role { get; set; }
        public ClientTarget? ClientTarget { get; set; }

        /// <summary>URL de la consola web (cliente de consola web).</summary>
        public string? WebConsoleUrl { get; set; }

        // Base de datos (servidor, o cliente de otro escritorio)
        public string? DbServer { get; set; }
        public string DbName { get; set; } = "SIFFA";
        public bool DbWindowsAuth { get; set; }
        public string? DbUser { get; set; }
        public bool DbTrustCertificate { get; set; } = true;

        /// <summary>Contraseña de SQL cifrada con DPAPI (solo la puede descifrar este usuario de Windows en este equipo).</summary>
        public string? DbPasswordProtected { get; set; }

        [JsonIgnore]
        public string? DbPassword
        {
            get => string.IsNullOrEmpty(DbPasswordProtected)
                ? null
                : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(DbPasswordProtected), null, DataProtectionScope.CurrentUser));
            set => DbPasswordProtected = string.IsNullOrEmpty(value)
                ? null
                : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
        }

        /// <summary>Este equipo usa SQL Server (es servidor, o cliente de otro escritorio).</summary>
        [JsonIgnore]
        public bool UsesDatabase => Role == MachineRole.Server || (Role == MachineRole.Client && ClientTarget == Services.ClientTarget.Desktop);

        /// <summary>¿Ya se terminó de configurar este equipo?</summary>
        [JsonIgnore]
        public bool IsConfigured => Role switch
        {
            MachineRole.Server => !string.IsNullOrWhiteSpace(DbServer),
            MachineRole.Client when ClientTarget == Services.ClientTarget.Desktop => !string.IsNullOrWhiteSpace(DbServer),
            MachineRole.Client when ClientTarget == Services.ClientTarget.WebConsole => !string.IsNullOrWhiteSpace(WebConsoleUrl),
            _ => false,
        };

        public string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = DbServer ?? string.Empty,
                InitialCatalog = string.IsNullOrWhiteSpace(DbName) ? "SIFFA" : DbName,
                TrustServerCertificate = DbTrustCertificate,
                ConnectTimeout = 8,
            };

            if (DbWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = DbUser ?? string.Empty;
                builder.Password = DbPassword ?? string.Empty;
            }

            return builder.ConnectionString;
        }

        /// <summary>Texto corto para mostrar en pantalla qué configuración tiene el equipo.</summary>
        public string Describe() => Role switch
        {
            MachineRole.Server => $"Servidor · {DbServer} / {DbName}",
            MachineRole.Client when ClientTarget == Services.ClientTarget.Desktop => $"Cliente de escritorio · {DbServer}",
            MachineRole.Client when ClientTarget == Services.ClientTarget.WebConsole => $"Cliente de consola web · {WebConsoleUrl}",
            _ => "Sin configurar",
        };
    }

    /// <summary>Lee y escribe machine.json y entrega la cadena de conexión que usa el DbContext.</summary>
    public class MachineSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string Folder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIFFA");

        private static string FilePath => Path.Combine(Folder, "machine.json");

        public MachineSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<MachineSettings>(File.ReadAllText(FilePath)) ?? new MachineSettings();
            }
            catch
            {
                // Archivo dañado: se trata como equipo sin configurar.
            }
            return new MachineSettings();
        }

        public void Save(MachineSettings settings)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
        }

        public void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        /// <summary>
        /// Cadena de conexión que se usa en cada ámbito: la del equipo si está configurada
        /// y, si no, la de appsettings.json (por ejemplo al desarrollar desde Visual Studio).
        /// </summary>
        public string ResolveConnectionString(IConfiguration fallback)
        {
            var settings = Load();
            if (settings.UsesDatabase && !string.IsNullOrWhiteSpace(settings.DbServer))
                return settings.BuildConnectionString();

            return fallback.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Este equipo no tiene configurada la conexión a la base de datos.");
        }
    }
}
