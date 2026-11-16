using SIFFA.Core.Models.Custom;

namespace SIFFA.Core.Services.UserAccountService
{
    /// <summary>Un usuario de acceso al sistema (Security.User) y si ya tiene credenciales para iniciar sesión.</summary>
    public record UserAccountItem(Guid UserId, string FirstName, string LastName, string Email, bool HasCredential, bool IsAdmin, DateTime CreatedAt)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>Alta y administración de los usuarios que pueden iniciar sesión (web y escritorio).</summary>
    public interface IUserAccountService
    {
        public const int MinPasswordLength = 8;

        Task<List<UserAccountItem>> GetAllAsync();

        /// <summary>Crea el usuario con sus credenciales de acceso; el correo es el nombre de usuario.</summary>
        Task<GenericReponse> CreateAsync(string firstName, string lastName, string email, string password);

        /// <summary>Asigna una contraseña nueva (también sirve para dar credenciales a un usuario que no las tiene).</summary>
        Task<GenericReponse> ResetPasswordAsync(Guid userId, string newPassword);

        /// <summary>Elimina el usuario y sus credenciales. No se puede eliminar al administrador ni a uno mismo.</summary>
        Task<GenericReponse> DeleteAsync(Guid userId, Guid? currentUserId);
    }
}
