namespace SIFFA.Core.Services.AuthService
{
    /// <summary>Resultado de validar un usuario y contraseña contra [Security].[Credential].</summary>
    public record AuthResult(bool IsSuccess, string Message, string? UserName = null, string? Email = null, Guid? UserId = null, string? Token = null);

    public interface IAuthService
    {
        /// <summary>
        /// Valida usuario (el correo) y contraseña con el mismo procedimiento que el login web:
        /// el usuario se busca por su hash y la contraseña se verifica con PBKDF2.
        /// </summary>
        Task<AuthResult> ValidateAsync(string? username, string? password);
    }
}
