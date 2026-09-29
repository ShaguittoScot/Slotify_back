namespace Slotify.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clientes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.SupabaseId)
            .HasColumnName("supabase_id");
        builder.HasIndex(x => x.SupabaseId).IsUnique();

        builder.Property(x => x.FirstName)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnName("apellido")
            .HasMaxLength(100);

        builder.Ignore(x => x.FullName);

        builder.Property(x => x.Email)
            .HasColumnName("correo")
            .HasMaxLength(150)
            .IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.Phone)
            .HasColumnName("telefono")
            .HasMaxLength(30);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relación 1:N con Citas
        builder.HasMany(x => x.Appointments)
            .WithOne(x => x.Client)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
