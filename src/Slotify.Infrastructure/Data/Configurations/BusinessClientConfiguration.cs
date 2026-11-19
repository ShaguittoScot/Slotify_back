namespace Slotify.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

public class BusinessClientConfiguration : IEntityTypeConfiguration<BusinessClient>
{
    public void Configure(EntityTypeBuilder<BusinessClient> builder)
    {
        builder.ToTable("clientes_negocio");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.ClientId)
            .HasColumnName("id_cliente");

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
            .HasMaxLength(150);

        builder.Property(x => x.Phone)
            .HasColumnName("telefono")
            .HasMaxLength(30);

        builder.Property(x => x.Notes)
            .HasColumnName("notas")
            .HasMaxLength(1000);

        builder.Property(x => x.ArchivedAt)
            .HasColumnName("fecha_archivado");

        builder.Property(x => x.HistoryAppointmentIds)
            .HasColumnName("citas_historial");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .IsConcurrencyToken()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.BusinessId, x.Id });
        builder.HasIndex(x => new { x.BusinessId, x.ClientId });
    }
}

