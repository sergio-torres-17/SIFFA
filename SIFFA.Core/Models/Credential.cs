namespace SIFFA.Core.Models;

public partial class Credential
{
    public Guid CredentialId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>SHA-256 del nombre de usuario normalizado (ver <c>UsernameHasher</c>).</summary>
    public byte[] UsernameHash { get; set; } = null!;

    /// <summary>Hash PBKDF2 con versión, iteraciones y sal incluidas (ver <c>EncryptionService</c>).</summary>
    public byte[] PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
