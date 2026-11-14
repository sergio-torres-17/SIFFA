using System.Buffers.Binary;
using System.Security.Cryptography;

namespace SIFFA.Core.Services.EncryptionService
{
    /// <summary>
    /// PBKDF2-HMACSHA256. Formato binario (53 bytes, cabe en VARBINARY(256)):
    /// [versión: 1][iteraciones: 4, big-endian][sal: 16][clave: 32].
    /// Guardar la versión y las iteraciones permite subir el costo más adelante sin invalidar hashes viejos.
    /// Es seguro registrarlo como singleton: no tiene estado ni toca la base de datos.
    /// </summary>
    public sealed class EncryptionService : IEncryptionService
    {
        private const byte Version = 1;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 210_000;
        private const int HeaderSize = 1 + 4;

        // Hash de relleno (contraseña aleatoria descartada) para igualar tiempos si el usuario no existe.
        private static readonly byte[] DummyHash = CreateHash(Guid.NewGuid().ToString());

        public byte[] HashPassword(string password) => CreateHash(password);

        public bool VerifyPassword(string password, byte[]? storedHash)
        {
            var matches = Check(password, storedHash ?? DummyHash);
            return matches && storedHash is not null;
        }

        private static byte[] CreateHash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            var result = new byte[HeaderSize + SaltSize + KeySize];
            result[0] = Version;
            BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(1, 4), Iterations);
            salt.CopyTo(result, HeaderSize);
            key.CopyTo(result, HeaderSize + SaltSize);
            return result;
        }

        private static bool Check(string password, byte[] storedHash)
        {
            if (storedHash.Length != HeaderSize + SaltSize + KeySize || storedHash[0] != Version)
                return false;

            var iterations = BinaryPrimitives.ReadInt32BigEndian(storedHash.AsSpan(1, 4));
            if (iterations <= 0)
                return false;

            var salt = storedHash.AsSpan(HeaderSize, SaltSize);
            var expected = storedHash.AsSpan(HeaderSize + SaltSize, KeySize);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
