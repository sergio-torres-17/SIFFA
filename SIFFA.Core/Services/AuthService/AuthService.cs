using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Security;
using SIFFA.Core.Services.EncryptionService;

namespace SIFFA.Core.Services.AuthService
{
    public class AuthService : IAuthService
    {
        public AuthService(SiffaContext db, IEncryptionService encryption)
        {
            Db = db;
            Encryption = encryption;
        }

        private SiffaContext Db { get; set; }
        private IEncryptionService Encryption { get; set; }

        public async Task<AuthResult> ValidateAsync(string? username, string? password)
        {
            var usernameHash = UsernameHasher.Hash(username ?? string.Empty);

            var credential = await Db.Credentials.AsNoTracking()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UsernameHash == usernameHash);

            // Se verifica siempre (con un hash de relleno si no existe) para no revelar por tiempo si el usuario existe.
            if (!Encryption.VerifyPassword(password ?? string.Empty, credential?.PasswordHash) || credential is null)
                return new AuthResult(false, "Usuario o contraseña incorrectos.");

            var user = credential.User;
            return new AuthResult(true, "Sesión iniciada.", $"{user.FirstName} {user.LastName}".Trim(), user.Email, user.UserId);
        }
    }
}
