namespace SIFFA.Core.Services.EncryptionService
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Convierte la contraseña en el valor que se guarda en <c>Security.Credential.PasswordHash</c>
        /// (VARBINARY(256)): hash de un solo sentido PBKDF2 con sal, de 53 bytes. No se puede descifrar.
        /// </summary>
        byte[] HashPassword(string password);

        /// <summary>
        /// Comprueba si la contraseña corresponde al hash guardado. Si <paramref name="storedHash"/> es
        /// <c>null</c> (usuario inexistente) hace igualmente el cálculo y devuelve <c>false</c>, para
        /// que el tiempo de respuesta no revele si el usuario existe.
        /// </summary>
        bool VerifyPassword(string password, byte[]? storedHash);
    }
}
