using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("sesiones_usuario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.UserId)
            .HasColumnName("id_usuario")
            .IsRequired();
        builder.HasIndex(x => x.UserId); // idx_sesiones_usuario

        builder.Property(x => x.RefreshTokenHash)
            .HasColumnName("token_refresco_hash")
            .HasMaxLength(255)
            .IsRequired();
        builder.HasIndex(x => x.RefreshTokenHash).IsUnique();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("fecha_expiracion")
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .HasColumnName("revocado")
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relaciones
        builder.HasOne(x => x.User)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
