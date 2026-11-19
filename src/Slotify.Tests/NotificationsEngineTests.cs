using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Notifications.Handlers;
using Slotify.Application.Features.Notifications.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class NotificationsEngineTests
{
    private readonly Mock<IAppointmentRepository> _apptRepoMock = new();
    private readonly Mock<IBusinessRepository> _bizRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();

    private GetUpcomingNotificationsHandler CreateHandler() =>
        new(_apptRepoMock.Object, _bizRepoMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task GetUpcoming_UnauthenticatedUser_ReturnsForbidden()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(u => u.UserId).Returns((Guid?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUpcomingNotificationsQuery(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetUpcoming_BusinessOwner_ReturnsAppointmentsAndComputesSnapshotHash()
    {
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.BusinessId).Returns(businessId);
        _currentUserMock.Setup(u => u.Role).Returns("DUENO");

        var appt1 = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(3),
            ClientName = "Carlos Barbero",
            Status = AppointmentStatus.Confirmed,
            AgreedTotal = 300m
        };

        _apptRepoMock.Setup(r => r.GetByDateRangeAsync(
            businessId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([appt1]);

        _bizRepoMock.Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Business { Id = businessId, Name = "Barbería Elite" });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUpcomingNotificationsQuery { HorizonDays = 14 }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Appointments.Should().HaveCount(1);
        result.Data.Appointments[0].ClientName.Should().Be("Carlos Barbero");
        result.Data.Appointments[0].BusinessName.Should().Be("Barbería Elite");
        result.Data.SnapshotHash.Should().NotBeNullOrWhiteSpace();
        result.Data.HorizonDays.Should().Be(14);
    }

    [Fact]
    public async Task GetUpcoming_ClientUser_ReturnsClientAppointments()
    {
        var clientId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(clientId);
        _currentUserMock.Setup(u => u.BusinessId).Returns((Guid?)null);
        _currentUserMock.Setup(u => u.Role).Returns("CLIENTE");

        var biz = new Business { Id = Guid.NewGuid(), Name = "Salón Belleza" };
        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = biz.Id,
            Business = biz,
            ClientId = clientId,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            ClientName = "Ana Cliente",
            Status = AppointmentStatus.Confirmed,
        };

        _apptRepoMock.Setup(r => r.GetByClientIdAsync(
            clientId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([appt]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetUpcomingNotificationsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Appointments.Should().HaveCount(1);
        result.Data.Appointments[0].BusinessName.Should().Be("Salón Belleza");
    }

    [Fact]
    public async Task GetUpcoming_ChangingAppointmentStatus_ChangesSnapshotHash()
    {
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.BusinessId).Returns(businessId);
        _currentUserMock.Setup(u => u.Role).Returns("DUENO");

        var aptId = Guid.NewGuid();
        var apptConfirmed = new Appointment
        {
            Id = aptId,
            BusinessId = businessId,
            StartTime = DateTime.UtcNow.AddHours(5),
            EndTime = DateTime.UtcNow.AddHours(6),
            ClientName = "Pedro",
            Status = AppointmentStatus.Confirmed
        };

        _apptRepoMock.Setup(r => r.GetByDateRangeAsync(
            businessId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([apptConfirmed]);

        var handler = CreateHandler();
        var result1 = await handler.Handle(new GetUpcomingNotificationsQuery(), CancellationToken.None);

        // Ahora la cita fue cancelada
        var apptCancelled = new Appointment
        {
            Id = aptId,
            BusinessId = businessId,
            StartTime = DateTime.UtcNow.AddHours(5),
            EndTime = DateTime.UtcNow.AddHours(6),
            ClientName = "Pedro",
            Status = AppointmentStatus.Cancelled,
            UpdatedAt = DateTime.UtcNow
        };

        _apptRepoMock.Setup(r => r.GetByDateRangeAsync(
            businessId,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([apptCancelled]);

        var result2 = await handler.Handle(new GetUpcomingNotificationsQuery(), CancellationToken.None);

        result1.Data!.SnapshotHash.Should().NotBe(result2.Data!.SnapshotHash);
    }
}
