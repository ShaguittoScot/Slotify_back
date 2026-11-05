using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class BusinessBrandConfiguration : IEntityTypeConfiguration<BusinessBrand>
{
    public void Configure(EntityTypeBuilder<BusinessBrand> builder)
    {
        builder.ToTable("marca_negocio");

        builder.HasKey(x => x.BusinessId);

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio");

        builder.Property(x => x.LogoUrl)
            .HasColumnName("url_logotipo");

        builder.Property(x => x.PrimaryColor)
            .HasColumnName("color_primario")
            .HasMaxLength(7)
            .HasDefaultValue("#000000");

        builder.Property(x => x.IsDefault)
            .HasColumnName("es_predeterminado")
            .HasDefaultValue(true);

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relación 1:1 con Business
        builder.HasOne(x => x.Business)
            .WithOne(x => x.Brand)
            .HasForeignKey<BusinessBrand>(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
