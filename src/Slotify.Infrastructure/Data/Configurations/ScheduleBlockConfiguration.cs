using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class ScheduleBlockConfiguration : IEntityTypeConfiguration<ScheduleBlock>
{
    public void Configure(EntityTypeBuilder<ScheduleBlock> builder)
    {
        builder.ToTable("bloqueos_agenda");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn(); // BIGSERIAL

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnName("id_empleado");

        builder.Property(x => x.StartDateTime)
            .HasColumnName("fecha_hora_inicio")
            .IsRequired();

        builder.Property(x => x.EndDateTime)
            .HasColumnName("fecha_hora_fin")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("motivo")
            .HasMaxLength(255);

        // Relaciones
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
