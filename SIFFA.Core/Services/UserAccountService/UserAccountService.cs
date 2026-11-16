using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Security;
using SIFFA.Core.Services.EncryptionService;
using System.Net.Mail;

namespace SIFFA.Core.Services.UserAccountService
{
    public class UserAccountService : IUserAccountService
    {
        private const string AdminEmail = "adminsiffa@multicodedev.com";
        private const int MaxName = 100;

        public UserAccountService(SiffaContext db, IEncryptionService encryption)
        {
            Db = db;
            Encryption = encryption;
        }

        private SiffaContext Db { get; }
        private IEncryptionService Encryption { get; }

        public async Task<List<UserAccountItem>> GetAllAsync() =>
            await Db.Users.AsNoTracking()
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new UserAccountItem(
                    u.UserId, u.FirstName, u.LastName, u.Email,
                    u.Credentials.Any(),
                    u.Email == AdminEmail,
                    u.CreatedAt))
                .ToListAsync();

        public async Task<GenericReponse> CreateAsync(string firstName, string lastName, string email, string password)
        {
            firstName = (firstName ?? "").Trim();
            lastName = (lastName ?? "").Trim();
            email = (email ?? "").Trim().ToLowerInvariant();

            if (firstName.Length == 0) return new(false, "El nombre es obligatorio.");
            if (lastName.Length == 0) return new(false, "Los apellidos son obligatorios.");
            if (firstName.Length > MaxName || lastName.Length > MaxName)
                return new(false, $"Nombre y apellidos admiten máximo {MaxName} caracteres.");
            if (!IsValidEmail(email)) return new(false, "El correo no es válido.");
            if (email.Length > 100) return new(false, "El correo admite máximo 100 caracteres.");

            var passwordError = ValidatePassword(password);
            if (passwordError is not null) return new(false, passwordError);

            // El correo es el nombre de usuario: no puede repetirse (sin distinguir mayúsculas).
            var usernameHash = UsernameHasher.Hash(email);
            var exists = await Db.Users.AnyAsync(u => u.Email.ToLower() == email)
                         || await Db.Credentials.AnyAsync(c => c.UsernameHash == usernameHash);
            if (exists) return new(false, "Ya existe un usuario con ese correo.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                CreatedAt = DateTime.Now,
            };
            user.Credentials.Add(new Credential
            {
                CredentialId = Guid.NewGuid(),
                UserId = user.UserId,
                UsernameHash = usernameHash,
                PasswordHash = Encryption.HashPassword(password),
                CreatedAt = DateTime.Now,
            });

            Db.Users.Add(user);
            await Db.SaveChangesAsync();
            return new(true, $"Usuario {email} creado.");
        }

        public async Task<GenericReponse> ResetPasswordAsync(Guid userId, string newPassword)
        {
            var passwordError = ValidatePassword(newPassword);
            if (passwordError is not null) return new(false, passwordError);

            var user = await Db.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return new(false, "El usuario ya no existe.");

            var hash = Encryption.HashPassword(newPassword);
            var credential = user.Credentials.FirstOrDefault();
            if (credential is null)
            {
                user.Credentials.Add(new Credential
                {
                    CredentialId = Guid.NewGuid(),
                    UserId = user.UserId,
                    UsernameHash = UsernameHasher.Hash(user.Email),
                    PasswordHash = hash,
                    CreatedAt = DateTime.Now,
                });
            }
            else
            {
                credential.PasswordHash = hash;
            }

            await Db.SaveChangesAsync();
            return new(true, $"Contraseña de {user.Email} actualizada.");
        }

        public async Task<GenericReponse> DeleteAsync(Guid userId, Guid? currentUserId)
        {
            var user = await Db.Users.Include(u => u.Credentials).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return new(false, "El usuario ya no existe.");
            if (user.Email == AdminEmail) return new(false, "El usuario administrador no se puede eliminar.");
            if (currentUserId == userId) return new(false, "No puedes eliminar tu propio usuario.");

            Db.Credentials.RemoveRange(user.Credentials);
            Db.Users.Remove(user);
            await Db.SaveChangesAsync();
            return new(true, $"Usuario {user.Email} eliminado.");
        }

        private static string? ValidatePassword(string? password) =>
            string.IsNullOrEmpty(password) || password.Length < IUserAccountService.MinPasswordLength
                ? $"La contraseña debe tener al menos {IUserAccountService.MinPasswordLength} caracteres."
                : null;

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { return new MailAddress(email).Address == email; }
            catch (FormatException) { return false; }
        }
    }
}
