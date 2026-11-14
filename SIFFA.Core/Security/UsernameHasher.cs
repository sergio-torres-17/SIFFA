using System.Security.Cryptography;
using System.Text;

namespace SIFFA.Core.Security;

/// <summary>
/// Hash determinista del nombre de usuario, para poder buscarlo en <c>Security.Credential.UsernameHash</c>.
/// Se normaliza (sin espacios en los extremos y sin distinguir mayúsculas) para que "Admin" y "admin " coincidan.
/// </summary>
public static class UsernameHasher
{
    public static byte[] Hash(string username)
    {
        var normalized = (username ?? string.Empty).Trim().ToUpperInvariant();
        return SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
    }
}
