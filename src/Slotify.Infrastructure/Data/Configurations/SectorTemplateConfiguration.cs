using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class SectorTemplateConfiguration : IEntityTypeConfiguration<SectorTemplate>
{
    public void Configure(EntityTypeBuilder<SectorTemplate> builder)
    {
        builder.ToTable("plantillas_sector");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseSerialColumn(); // PostgreSQL SERIAL

        builder.Property(x => x.Name)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DefaultModules)
            .HasColumnName("modulos_defecto")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.Property(x => x.SuggestedServices)
            .HasColumnName("servicios_sugeridos")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.Property(x => x.FormConfig)
            .HasColumnName("configuracion_formulario")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
    }
}
