using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("servicios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.DurationMinutes)
            .HasColumnName("duracion_minutos")
            .IsRequired();

        builder.Property(x => x.BufferTimeMinutes)
            .HasColumnName("tiempo_colchon_minutos")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("precio")
            .HasPrecision(10, 2)
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("esta_activo")
            .HasDefaultValue(true);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("fecha_eliminacion")
            .IsRequired(false);

        // Relación N:1 con Business
        builder.HasOne(x => x.Business)
            .WithMany(x => x.Services)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
