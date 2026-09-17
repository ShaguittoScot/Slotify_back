using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;

namespace Slotify.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasColumnName("nombre_completo")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("correo")
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();


        builder.Property(x => x.Role)
            .HasColumnName("rol")
            .HasMaxLength(20)
            .HasConversion(
                v => v == UserRole.Owner ? "DUENO" : "EMPLEADO",
                v => v == "DUENO" ? UserRole.Owner : UserRole.Employee
            )
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("esta_activo")
            .HasDefaultValue(true);

        builder.Property(x => x.EmailVerified)
            .HasColumnName("correo_verificado")
            .HasDefaultValue(false);

        builder.Property(x => x.LastLogin)
            .HasColumnName("ultimo_inicio_sesion");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relaciones
        builder.HasOne(x => x.Business)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
