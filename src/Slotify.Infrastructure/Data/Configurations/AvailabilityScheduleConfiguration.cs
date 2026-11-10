
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public class AvailabilityScheduleConfiguration
    : IEntityTypeConfiguration<AvailabilitySchedule>
{
    public void Configure(EntityTypeBuilder<AvailabilitySchedule> builder)
    {
        builder.ToTable("horarios_disponibilidad");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnName("id_empleado")
            .IsRequired(false);

        builder.Property(x => x.DayOfWeek)
            .HasColumnName("dia_semana")
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnName("hora_inicio")
            .HasColumnType("time without time zone")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("hora_fin")
            .HasColumnType("time without time zone")
            .IsRequired();

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
