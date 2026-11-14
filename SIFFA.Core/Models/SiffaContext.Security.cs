using Microsoft.EntityFrameworkCore;

namespace SIFFA.Core.Models;

// Mapeo del esquema Security en un archivo aparte para que no se pierda al regenerar el scaffold.
public partial class SiffaContext
{
    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "Security");

            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Email, "UX_User_Email").IsUnique();

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.ToTable("Credential", "Security");

            entity.HasKey(e => e.CredentialId);

            entity.HasIndex(e => e.UserId, "IX_Credential_UserId");
            entity.HasIndex(e => e.UsernameHash, "UX_Credential_UsernameHash").IsUnique();

            entity.Property(e => e.CredentialId).ValueGeneratedNever();
            entity.Property(e => e.UsernameHash).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Credentials)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_Credential_UserId");
        });
    }
}
