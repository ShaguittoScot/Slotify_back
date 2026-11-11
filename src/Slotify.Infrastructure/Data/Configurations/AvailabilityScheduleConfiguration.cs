using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;

namespace Slotify.Infrastructure.Data.Configurations;

public sealed class AvailabilityScheduleConfiguration : IEntityTypeConfiguration<AvailabilitySchedule>
{
    public void Configure(EntityTypeBuilder<AvailabilitySchedule> builder)
    {
        builder.ToTable("horarios_disponibilidad");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BusinessId).HasColumnName("id_negocio");
        builder.Property(x => x.EmployeeId).HasColumnName("id_empleado");
        builder.Property(x => x.DayOfWeek).HasColumnName("dia_semana");
        builder.Property(x => x.StartTime).HasColumnName("hora_inicio");
        builder.Property(x => x.EndTime).HasColumnName("hora_fin");
        builder.HasOne(x => x.Business).WithMany().HasForeignKey(x => x.BusinessId);
    }
}
