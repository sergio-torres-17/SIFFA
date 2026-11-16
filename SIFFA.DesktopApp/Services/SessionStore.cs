using System.IO;
using System.Text.Json;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>Sesión iniciada en este equipo. Se guarda en %LOCALAPPDATA%\SIFFA\session.json.</summary>
    public class UserSession
    {
        public Guid? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime SignedInAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class SessionStore
    {
        /// <summary>Horas que dura una sesión antes de pedir login otra vez.</summary>
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

        private static string FilePath => Path.Combine(MachineSettingsStore.Folder, "session.json");

        /// <summary>La sesión guardada, o null si no hay o ya venció.</summary>
        public UserSession? Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                var session = JsonSerializer.Deserialize<UserSession>(File.ReadAllText(FilePath));
                return session is not null && session.ExpiresAt > DateTime.Now ? session : null;
            }
            catch
            {
                return null;
            }
        }

        public UserSession Save(Guid? userId, string name, string email)
        {
            var session = new UserSession
            {
                UserId = userId,
                Name = name,
                Email = email,
                SignedInAt = DateTime.Now,
                ExpiresAt = DateTime.Now + Lifetime,
            };

            Directory.CreateDirectory(MachineSettingsStore.Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(session));
            return session;
        }

        public void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
