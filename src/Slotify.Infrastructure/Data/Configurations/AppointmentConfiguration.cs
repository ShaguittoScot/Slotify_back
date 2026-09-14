using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;

namespace Slotify.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("citas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(x => x.BusinessId)
            .HasColumnName("id_negocio")
            .IsRequired();

        builder.Property(x => x.EmployeeId)
            .HasColumnName("id_empleado");

        builder.Property(x => x.StartTime)
            .HasColumnName("hora_inicio")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("hora_fin")
            .IsRequired();

        builder.Property(x => x.ClientName)
            .HasColumnName("nombre_cliente")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ClientEmail)
            .HasColumnName("correo_cliente")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ClientPhone)
            .HasColumnName("telefono_cliente")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("estado")
            .HasMaxLength(20)
            .HasConversion(
                v => v == AppointmentStatus.Confirmed ? "confirmed" :
                     v == AppointmentStatus.Cancelled ? "cancelled" :
                     v == AppointmentStatus.Completed ? "completed" : "no_show",
                v => v == "confirmed" ? AppointmentStatus.Confirmed :
                     v == "cancelled" ? AppointmentStatus.Cancelled :
                     v == "completed" ? AppointmentStatus.Completed : AppointmentStatus.NoShow
            )
            .HasDefaultValue(AppointmentStatus.Confirmed)
            .IsRequired();

        builder.Property(x => x.CancellationToken)
            .HasColumnName("token_cancelacion")
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(x => x.CancellationToken).IsUnique();

        builder.Property(x => x.AgreedTotal)
            .HasColumnName("total_pactado")
            .HasColumnType("numeric(10,2)")
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relaciones
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => new { x.BusinessId, x.StartTime, x.EndTime }); // idx_citas_calendario
        builder.HasIndex(x => new { x.EmployeeId, x.StartTime, x.EndTime }); // idx_citas_empleado
    }
}
