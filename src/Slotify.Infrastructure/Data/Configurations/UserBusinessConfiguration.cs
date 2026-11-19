namespace Slotify.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;

public class UserBusinessConfiguration : IEntityTypeConfiguration<UserBusiness>
{
    public void Configure(EntityTypeBuilder<UserBusiness> builder)
    {
        builder.ToTable("usuarios_negocios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.UserId)
            .HasColumnName("id_usuario")
            .IsRequired();

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        // Conversor explícito que valida estrictamente los valores sin transformaciones silenciosas
        var roleConverter = new ValueConverter<UserRole, string>(
            v => RoleToString(v),
            v => StringToRole(v)
        );

        builder.Property(x => x.Role)
            .HasColumnName("rol")
            .HasMaxLength(20)
            .HasConversion(roleConverter)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("esta_activo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Restricción de unicidad: un usuario no puede tener más de una vinculación con el mismo negocio
        builder.HasIndex(x => new { x.UserId, x.BusinessId })
            .IsUnique();

        // Relaciones con eliminación en cascada de la membresía (sin borrar usuario ni negocio)
        builder.HasOne(x => x.User)
            .WithMany(u => u.UserBusinesses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Business)
            .WithMany(b => b.UserBusinesses)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static string RoleToString(UserRole role) =>
        role switch
        {
            UserRole.Owner => "DUENO",
            UserRole.Employee => "EMPLEADO",
            _ => throw new ArgumentOutOfRangeException(nameof(role), $"Rol de usuario no soportado: {role}")
        };

    private static UserRole StringToRole(string role) =>
        role switch
        {
            "DUENO" => UserRole.Owner,
            "EMPLEADO" => UserRole.Employee,
            _ => throw new InvalidOperationException($"Valor de rol desconocido en base de datos: {role}")
        };
}
