using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Calendar.Commands;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class CreateAppointmentTests
{
    private readonly CreateAppointmentValidator _validator = new();

    [Fact]
    public void Validator_DeberiaFallar_CuandoClientNameEsVacio()
    {
        // Arrange
        var command = new CreateAppointmentCommand
        {
            ClientName = "",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            AgreedTotal = 250m
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentCommand.ClientName));
    }

    [Fact]
    public void Validator_DeberiaFallar_CuandoStartTimeEsPosteriorAEndTime()
    {
        // Arrange
        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            StartTime = DateTime.UtcNow.AddHours(3),
            EndTime = DateTime.UtcNow.AddHours(1),
            AgreedTotal = 250m
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentCommand.StartTime));
    }

    [Fact]
    public void Validator_DeberiaAprobar_CuandoDatosSonCorrectos()
    {
        // Arrange
        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            ClientEmail = "carlos@ejemplo.com",
            ClientPhone = "+52 55 1234 5678",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            AgreedTotal = 280m
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_DeberiaCrearCita_UsandoBusinessIdDelToken_YRetornarResultOk()
    {
        // Arrange
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockScheduleBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var existingBusiness = new Business { Id = businessId, Name = "Barbería Estilo" };
        mockBizRepo.Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        mockScheduleBlockRepo.Setup(s => s.HasConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        mockApptRepo.Setup(a => a.HasActiveAppointmentConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        mockApptRepo.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);

        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateAppointmentHandler(
            mockApptRepo.Object,
            mockBizRepo.Object,
            mockScheduleBlockRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            ClientEmail = "carlos@ejemplo.com",
            ClientPhone = "+52 55 1234 5678",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            AgreedTotal = 280m
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        mockApptRepo.Verify(
            a => a.AddAsync(
                It.Is<Appointment>(x => x.ClientName == "Carlos Mendoza" && x.BusinessId == businessId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handler_DeberiaFallar_CuandoHorarioEstaBloqueadoPorAgenda()
    {
        // Arrange
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockScheduleBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var existingBusiness = new Business { Id = businessId, Name = "Barbería Estilo" };
        mockBizRepo.Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        // Simula conflicto con un bloqueo manual de agenda
        mockScheduleBlockRepo.Setup(s => s.HasConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreateAppointmentHandler(
            mockApptRepo.Object,
            mockBizRepo.Object,
            mockScheduleBlockRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            AgreedTotal = 280m
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("bloqueo de agenda"));
        mockApptRepo.Verify(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handler_DeberiaFallar_CuandoTokenNoTieneBusinessId()
    {
        // Arrange
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockScheduleBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(c => c.BusinessId).Returns((Guid?)null);

        var handler = new CreateAppointmentHandler(
            mockApptRepo.Object,
            mockBizRepo.Object,
            mockScheduleBlockRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            AgreedTotal = 280m
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        mockApptRepo.Verify(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
