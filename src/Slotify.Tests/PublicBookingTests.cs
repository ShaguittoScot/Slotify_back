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

        var businessClients = new Mock<IBusinessClientRepository>();
        var uow = new Mock<IUnitOfWork>();
        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            appointments.Object,
            Mock.Of<IClientRepository>(),
            businessClients.Object,
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
        added.ClientId.Should().BeNull();
        added.EndTime.Should().Be(start.AddMinutes(45));
        added.Status.Should().Be(AppointmentStatus.Confirmed);
        businessClients.Verify(r => r.AddAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()), Times.Never);
        businessClients.Verify(r => r.UpdateAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        result.Data!.BusinessSlug.Should().Be("barberia-centro");
        result.Data.IsRegisteredClient.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePublicAppointment_RegisteredSlotifyAccount_AutoIntegratesIntoOfficialDirectoryWithoutDuplicates()
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Barbería Centro", Slug = "barberia-centro" };
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(b => b.GetBySlugAsync("barberia-centro", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var supabaseUserId = Guid.NewGuid();
        var globalClient = new Client
        {
            Id = supabaseUserId,
            SupabaseId = supabaseUserId,
            FirstName = "Sofía",
            LastName = "Ramírez",
            Email = "sofia@slotify.app",
            Phone = "4429998877"
        };
        var clientRepo = new Mock<IClientRepository>();
        clientRepo.Setup(c => c.GetBySupabaseIdAsync(supabaseUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(globalClient);

        var storedAppointments = new List<Appointment>();
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, DateTime start, DateTime end, CancellationToken _) =>
                storedAppointments.Where(a => a.StartTime < end && a.EndTime > start).ToList());
        appointments.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>((a, _) => storedAppointments.Add(a))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        var storedBusinessClients = new List<BusinessClient>();
        var businessClients = new Mock<IBusinessClientRepository>();
        businessClients.Setup(r => r.GetByClientIdAndBusinessIdAsync(globalClient.Id, business.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storedBusinessClients.FirstOrDefault(c => c.BusinessId == business.Id && c.ClientId == globalClient.Id));
        businessClients.Setup(r => r.GetByBusinessIdAsync(business.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storedBusinessClients.Where(c => c.BusinessId == business.Id).ToList());
        businessClients.Setup(r => r.AddAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessClient, CancellationToken>((c, _) => storedBusinessClients.Add(c))
            .ReturnsAsync((BusinessClient c, CancellationToken _) => c);

        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(b => b.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var uow = new Mock<IUnitOfWork>();
        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            appointments.Object,
            clientRepo.Object,
            businessClients.Object,
            uow.Object,
            new CalendarAvailability(appointments.Object, blocks.Object),
            new TestCalendarDatabase());

        var start1 = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc);
        var res1 = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "barberia-centro",
            StartTime = start1,
            DurationMinutes = 30,
            ClientName = "Sofía Ramírez",
            ClientPhone = "4429998877",
            ClientId = supabaseUserId,
            IsVerifiedAccount = true,
            VerifiedEmail = "sofia@slotify.app",
            AgreedTotal = 300m,
        }, CancellationToken.None);

        res1.Success.Should().BeTrue();
        res1.Data!.IsRegisteredClient.Should().BeTrue();
        storedBusinessClients.Should().ContainSingle();
        var card = storedBusinessClients[0];
        card.BusinessId.Should().Be(business.Id);
        card.ClientId.Should().Be(globalClient.Id);
        card.Email.Should().Be("sofia@slotify.app");
        card.HistoryAppointmentIds.Should().Equal(res1.Data.Id);

        // Segunda cita con la misma cuenta registrada: reutiliza ficha sin duplicar
        var start2 = start1.AddDays(1);
        var res2 = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "barberia-centro",
            StartTime = start2,
            DurationMinutes = 30,
            ClientName = "Sofía Ramírez",
            ClientId = supabaseUserId,
            IsVerifiedAccount = true,
            VerifiedEmail = "sofia@slotify.app",
            AgreedTotal = 350m,
        }, CancellationToken.None);

        res2.Success.Should().BeTrue();
        storedBusinessClients.Should().ContainSingle();
        card.HistoryAppointmentIds.Should().Equal(res1.Data.Id, res2.Data!.Id);
    }

    [Fact]
    public async Task CreatePublicAppointment_VerifiedSupabaseUserWithoutLocalClientRow_CreatesClientAndDirectoryCard()
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Spa Sol", Slug = "spa-sol" };
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(b => b.GetBySlugAsync("spa-sol", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var supabaseUserId = Guid.NewGuid();
        var createdClients = new List<Client>();
        var clientRepo = new Mock<IClientRepository>();
        clientRepo.Setup(c => c.AddAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((c, _) => createdClients.Add(c))
            .ReturnsAsync((Client c, CancellationToken _) => c);

        var storedCards = new List<BusinessClient>();
        var businessClients = new Mock<IBusinessClientRepository>();
        businessClients.Setup(r => r.AddAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessClient, CancellationToken>((c, _) => storedCards.Add(c))
            .ReturnsAsync((BusinessClient c, CancellationToken _) => c);

        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(a => a.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        appointments.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(b => b.GetByDateRangeAsync(business.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new CreatePublicAppointmentHandler(
            businesses.Object,
            appointments.Object,
            clientRepo.Object,
            businessClients.Object,
            Mock.Of<IUnitOfWork>(),
            new CalendarAvailability(appointments.Object, blocks.Object),
            new TestCalendarDatabase());

        var result = await handler.Handle(new CreatePublicAppointmentCommand
        {
            Slug = "spa-sol",
            StartTime = new DateTime(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 60,
            ClientName = "Diego Morales",
            ClientId = supabaseUserId,
            IsVerifiedAccount = true,
            VerifiedEmail = "diego@slotify.app",
            VerifiedFirstName = "Diego",
            VerifiedLastName = "Morales",
            AgreedTotal = 500m,
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsRegisteredClient.Should().BeTrue();
        createdClients.Should().ContainSingle(c => c.Id == supabaseUserId && c.Email == "diego@slotify.app");
        storedCards.Should().ContainSingle(c => c.ClientId == supabaseUserId && c.BusinessId == business.Id);
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
            Mock.Of<IBusinessClientRepository>(),
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
            Mock.Of<IBusinessClientRepository>(),
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
