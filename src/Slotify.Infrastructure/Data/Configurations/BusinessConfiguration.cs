using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("negocios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.Property(x => x.Name)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("telefono")
            .HasMaxLength(20);

        builder.Property(x => x.TimeZone)
            .HasColumnName("zona_horaria")
            .HasMaxLength(50)
            .HasDefaultValue("America/Mexico_City")
            .IsRequired();

        builder.Property(x => x.MinAdvanceHours)
            .HasColumnName("anticipacion_minima_horas")
            .HasDefaultValue(2)
            .IsRequired();

        builder.Property(x => x.MaxAdvanceDays)
            .HasColumnName("anticipacion_maxima_dias")
            .HasDefaultValue(30)
            .IsRequired();

        builder.Property(x => x.SectorTemplateId)
            .HasColumnName("id_plantilla_sector");

        builder.Property(x => x.CustomFormConfig)
            .HasColumnName("configuracion_formulario_personalizada")
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relaciones
        builder.HasOne(x => x.SectorTemplate)
            .WithMany(x => x.Businesses)
            .HasForeignKey(x => x.SectorTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
