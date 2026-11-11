using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Calendar.Commands;
using Slotify.Application.Features.Calendar;
using Slotify.Application.Common.Models;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class CreateAppointmentTests
{
    private readonly CreateAppointmentValidator _validator = new();

    private static CalendarAvailability Availability(IAppointmentRepository appointments) =>
        new(appointments, new Mock<IScheduleBlockRepository>().Object);

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

    [Theory]
    [InlineData(null, null, "", "")]
    [InlineData(" ", " ", "", "")]
    [InlineData(" carlos@ejemplo.com ", " 4421234567 ", "carlos@ejemplo.com", "4421234567")]
    public async Task Handler_DeberiaCrearCita_ConBusinessIdDelTokenYContactoProvisto(
        string? email, string? phone, string expectedEmail, string expectedPhone)
    {
        // Arrange
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var existingBusiness = new Business { Id = businessId, Name = "Barbería Estilo" };
        mockBizRepo.Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBusiness);

        mockApptRepo.Setup(a => a.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        mockApptRepo.Setup(a => a.GetByDateRangeAsync(businessId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());

        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new CreateAppointmentHandler(
            mockApptRepo.Object,
            mockBizRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object,
            new CalendarAvailability(mockApptRepo.Object, EmptyBlocks(businessId).Object),
            new TestCalendarDatabase(),
            Mock.Of<IBusinessClientRepository>());

        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            ClientEmail = email,
            ClientPhone = phone,
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
                It.Is<Appointment>(x =>
                    x.ClientName == "Carlos Mendoza" && x.BusinessId == businessId &&
                    x.ClientEmail == expectedEmail && x.ClientPhone == expectedPhone),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_DeberiaRechazar_ContactoMasLargoQueLasColumnasDeCitas()
    {
        var now = DateTime.UtcNow;
        var command = new CreateAppointmentCommand
        {
            ClientName = "Carlos Mendoza",
            ClientEmail = $"{new string('a', 151)}@ejemplo.com",
            ClientPhone = new string('1', 21),
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2)
        };

        var result = _validator.Validate(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentCommand.ClientEmail));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentCommand.ClientPhone));
    }

    [Fact]
    public async Task Handler_DeberiaFallar_CuandoTokenNoTieneBusinessId()
    {
        // Arrange
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(c => c.BusinessId).Returns((Guid?)null);

        var handler = new CreateAppointmentHandler(
            mockApptRepo.Object,
            mockBizRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object,
            Availability(mockApptRepo.Object),
            new TestCalendarDatabase(),
            Mock.Of<IBusinessClientRepository>());

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoGuardaCitaSinEmpleadoSobreOtraCitaOBloqueo(bool blocked)
    {
        var businessId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var appointments = new Mock<IAppointmentRepository>();
        var blocks = EmptyBlocks(businessId);
        var businesses = new Mock<IBusinessRepository>();
        var user = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        user.Setup(u => u.BusinessId).Returns(businessId);
        businesses.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>())).ReturnsAsync(new Business());
        appointments.Setup(r => r.GetByDateRangeAsync(businessId, now, now.AddHours(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocked ? [] : [new Appointment { EmployeeId = Guid.NewGuid() }]);
        if (blocked)
        {
            blocks.Setup(r => r.GetByDateRangeAsync(businessId, now, now.AddHours(1), It.IsAny<CancellationToken>()))
                .ReturnsAsync([new ScheduleBlock { EmployeeId = Guid.NewGuid() }]);
        }

        var handler = new CreateAppointmentHandler(appointments.Object, businesses.Object, unitOfWork.Object,
            user.Object, new CalendarAvailability(appointments.Object, blocks.Object), new TestCalendarDatabase(),
            Mock.Of<IBusinessClientRepository>());
        var result = await handler.Handle(new CreateAppointmentCommand
        {
            ClientName = "Cliente de prueba",
            StartTime = now,
            EndTime = now.AddHours(1)
        }, CancellationToken.None);

        result.ErrorType.Should().Be(ErrorType.Conflict);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IScheduleBlockRepository> EmptyBlocks(Guid businessId)
    {
        var blocks = new Mock<IScheduleBlockRepository>();
        blocks.Setup(r => r.GetByDateRangeAsync(businessId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScheduleBlock>());
        return blocks;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task AgendarParaFicha_EnlazaLaCitaSoloSiLaFichaEstaActivaYEsDelNegocio(bool archived, bool foreign)
    {
        var businessId = Guid.NewGuid();
        var card = new BusinessClient
        {
            Id = Guid.NewGuid(), BusinessId = foreign ? Guid.NewGuid() : businessId,
            ArchivedAt = archived ? DateTime.UtcNow : null
        };
        var appointments = new Mock<IAppointmentRepository>();
        var businesses = new Mock<IBusinessRepository>();
        var clients = new Mock<IBusinessClientRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var start = DateTime.UtcNow.AddDays(1);
        currentUser.Setup(user => user.BusinessId).Returns(businessId);
        businesses.Setup(repo => repo.GetByIdAsync(businessId, It.IsAny<CancellationToken>())).ReturnsAsync(new Business());
        appointments.Setup(repo => repo.GetByDateRangeAsync(businessId, start, start.AddHours(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
        clients.Setup(repo => repo.GetByIdAndBusinessIdAsync(card.Id, businessId, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        var handler = new CreateAppointmentHandler(appointments.Object, businesses.Object, unitOfWork.Object,
            currentUser.Object, new CalendarAvailability(appointments.Object, EmptyBlocks(businessId).Object),
            new TestCalendarDatabase(), clients.Object);

        var result = await handler.Handle(new CreateAppointmentCommand
        {
            BusinessClientId = card.Id, ClientName = "Cliente registrado", StartTime = start, EndTime = start.AddHours(1)
        }, CancellationToken.None);

        if (archived || foreign)
        {
            result.ErrorType.Should().Be(ErrorType.NotFound);
            card.HistoryAppointmentIds.Should().BeEmpty();
            unitOfWork.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            result.Success.Should().BeTrue();
            card.HistoryAppointmentIds.Should().ContainSingle().Which.Should().Be(result.Data);
            appointments.Verify(repo => repo.AddAsync(It.Is<Appointment>(a => a.Id == result.Data && a.ClientId == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
