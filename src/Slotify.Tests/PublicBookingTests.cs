using FluentAssertions;
using Moq;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Calendar;
using Slotify.Application.Features.PublicBooking;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public sealed class PublicBookingTests
{
    [Fact]
    public async Task CreatePublicAppointment_PersistsAppointmentAndReturnsConfirmation()
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Barbería Centro", Slug = "barberia-centro" };
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(b => b.GetBySlugAsync("barberia-centro", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(b => b.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Appointment? added = null;
        appointments.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>((a, _) => added = a)
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        var uow = new Mock<IUnitOfWork>();
        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            appointments.Object,
            Mock.Of<IClientRepository>(),
            uow.Object,
            new CalendarAvailability(appointments.Object, blocks.Object),
            new TestCalendarDatabase());

        var start = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc);
        var result = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "barberia-centro",
            StartTime = start,
            DurationMinutes = 45,
            ClientName = "Andrea López",
            ClientPhone = "4421112233",
            ClientEmail = "andrea@example.com",
            AgreedTotal = 350m,
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        added.Should().NotBeNull();
        added!.BusinessId.Should().Be(business.Id);
        added.EndTime.Should().Be(start.AddMinutes(45));
        added.Status.Should().Be(AppointmentStatus.Confirmed);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Data!.BusinessSlug.Should().Be("barberia-centro");
    }

    [Fact]
    public async Task CreatePublicAppointment_WhenSlotOccupied_ReturnsConflict()
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Barbería", Slug = "barberia" };
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(b => b.GetBySlugAsync("barberia", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var start = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc);
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    StartTime = start,
                    EndTime = start.AddMinutes(30),
                    Status = AppointmentStatus.Confirmed,
                }
            ]);

        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(b => b.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var uow = new Mock<IUnitOfWork>();
        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            appointments.Object,
            Mock.Of<IClientRepository>(),
            uow.Object,
            new CalendarAvailability(appointments.Object, blocks.Object),
            new TestCalendarDatabase());

        var result = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "barberia",
            StartTime = start,
            DurationMinutes = 30,
            ClientName = "Cliente",
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePublicAppointment_UnknownSlug_ReturnsNotFound()
    {
        var businesses = new Mock<IBusinessRepository>();
        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            Mock.Of<IAppointmentRepository>(),
            Mock.Of<IClientRepository>(),
            Mock.Of<IUnitOfWork>(),
            new CalendarAvailability(Mock.Of<IAppointmentRepository>(), Mock.Of<IScheduleBlockRepository>()),
            new TestCalendarDatabase());

        var result = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "no-existe",
            StartTime = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc),
            ClientName = "Cliente",
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetPublicAvailability_ReturnsNonCancelledIntervalsAndBlocksWithoutPii()
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Slug = "spa-luz",
            TimeZone = "America/Mexico_City",
        };
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(b => b.GetBySlugAsync("spa-luz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var schedules = new Mock<IAvailabilityScheduleRepository>();
        schedules.Setup(s => s.GetByBusinessIdAsync(business.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AvailabilitySchedule { BusinessId = business.Id, DayOfWeek = 5, StartTime = new(9, 0), EndTime = new(18, 0) },
            ]);

        var t1 = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 20, 18, 0, 0, DateTimeKind.Utc);
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Appointment { StartTime = t1, EndTime = t1.AddMinutes(30), Status = AppointmentStatus.Confirmed, ClientName = "Privado" },
                new Appointment { StartTime = t1.AddHours(1), EndTime = t1.AddHours(2), Status = AppointmentStatus.Cancelled },
            ]);

        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(b => b.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ScheduleBlock { StartDateTime = t2, EndDateTime = t2.AddMinutes(60) },
            ]);

        var handler = new GetPublicAvailabilityHandler(businesses.Object, schedules.Object, appointments.Object, blocks.Object);
        var result = await handler.Handle(new GetPublicAvailabilityQuery("spa-luz", new DateOnly(2026, 11, 20)), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Occupied.Should().HaveCount(2);
        result.Data.Hours.Should().ContainSingle();
    }

    [Fact]
    public async Task GetConsumerAppointments_ReturnsNonCancelledAppointmentsForConsumer()
    {
        var supaId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var clients = new Mock<IClientRepository>();
        clients.Setup(c => c.GetBySupabaseIdAsync(supaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Client { Id = clientId, Email = "cliente@example.com" });

        var biz = new Business { Id = Guid.NewGuid(), Name = "Barbería Nova", Slug = "barberia-nova" };
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByConsumerIdentityAsync(clientId, "cliente@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Business = biz,
                    ServiceTitle = "Corte Clásico",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
                    Status = AppointmentStatus.Confirmed,
                    AgreedTotal = 200m,
                },
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    Business = biz,
                    Status = AppointmentStatus.Cancelled,
                },
            ]);

        var handler = new GetConsumerAppointmentsHandler(appointments.Object, clients.Object);
        var result = await handler.Handle(new GetConsumerAppointmentsQuery(supaId, "cliente@example.com"), CancellationToken.None);

        result.Success.Should().BeTrue();
        var item = Assert.Single(result.Data!);
        item.BusinessName.Should().Be("Barbería Nova");
        item.Title.Should().Be("Corte Clásico");
        item.Status.Should().Be("confirmed");
    }
}
