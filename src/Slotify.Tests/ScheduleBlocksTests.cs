using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.ScheduleBlocks.Commands;
using Slotify.Application.Features.ScheduleBlocks.DTOs;
using Slotify.Application.Features.ScheduleBlocks.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class ScheduleBlocksTests
{
    private readonly CreateScheduleBlockValidator _validator = new();

    [Fact]
    public void Validator_DeberiaFallar_CuandoStartDateTimeEsPosteriorAEndDateTime()
    {
        // Arrange
        var command = new CreateScheduleBlockCommand
        {
            StartDateTime = DateTime.UtcNow.AddHours(2),
            EndDateTime = DateTime.UtcNow.AddHours(1),
            Reason = "Almuerzo"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateScheduleBlockCommand.StartDateTime));
    }

    [Fact]
    public void Validator_DeberiaAprobar_CuandoDatosSonValidos()
    {
        // Arrange
        var command = new CreateScheduleBlockCommand
        {
            StartDateTime = DateTime.UtcNow.AddHours(1),
            EndDateTime = DateTime.UtcNow.AddHours(2),
            Reason = "Horario de Comida"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateHandler_DeberiaCrearBloqueo_YPersistirConUnitOfWork()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        mockApptRepo.Setup(a => a.HasActiveAppointmentConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        mockBlockRepo.Setup(b => b.HasConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        mockBlockRepo.Setup(b => b.AddAsync(It.IsAny<ScheduleBlock>(), It.IsAny<CancellationToken>()))
            .Callback<ScheduleBlock, CancellationToken>((b, _) => b.Id = 101)
            .ReturnsAsync((ScheduleBlock b, CancellationToken _) => b);

        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateScheduleBlockHandler(
            mockBlockRepo.Object,
            mockApptRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateScheduleBlockCommand
        {
            StartDateTime = DateTime.UtcNow.AddHours(2),
            EndDateTime = DateTime.UtcNow.AddHours(3),
            Reason = "Mantenimiento Técnico"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(101);
        mockBlockRepo.Verify(b => b.AddAsync(It.Is<ScheduleBlock>(x => x.BusinessId == businessId && x.Reason == "Mantenimiento Técnico"), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHandler_DeberiaFallar_CuandoExisteCitaActivaEnElHorario()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        mockApptRepo.Setup(a => a.HasActiveAppointmentConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreateScheduleBlockHandler(
            mockBlockRepo.Object,
            mockApptRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateScheduleBlockCommand
        {
            StartDateTime = DateTime.UtcNow.AddHours(2),
            EndDateTime = DateTime.UtcNow.AddHours(3),
            Reason = "Horario de Comida"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("citas confirmadas"));
        mockBlockRepo.Verify(b => b.AddAsync(It.IsAny<ScheduleBlock>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateHandler_DeberiaFallar_CuandoExisteBloqueoSolapado()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockApptRepo = new Mock<IAppointmentRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        mockApptRepo.Setup(a => a.HasActiveAppointmentConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        mockBlockRepo.Setup(b => b.HasConflictAsync(
            businessId,
            It.IsAny<Guid?>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<long?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new CreateScheduleBlockHandler(
            mockBlockRepo.Object,
            mockApptRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        var command = new CreateScheduleBlockCommand
        {
            StartDateTime = DateTime.UtcNow.AddHours(2),
            EndDateTime = DateTime.UtcNow.AddHours(3),
            Reason = "Descanso"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Ya existe un bloqueo"));
        mockBlockRepo.Verify(b => b.AddAsync(It.IsAny<ScheduleBlock>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteHandler_DeberiaEliminarBloqueo_YPersistirConUnitOfWork()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var existingBlock = new ScheduleBlock
        {
            Id = 55,
            BusinessId = businessId,
            StartDateTime = DateTime.UtcNow.AddHours(1),
            EndDateTime = DateTime.UtcNow.AddHours(2),
            Reason = "Almuerzo"
        };

        mockBlockRepo.Setup(b => b.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlock);

        mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteScheduleBlockHandler(
            mockBlockRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        // Act
        var result = await handler.Handle(new DeleteScheduleBlockCommand { BlockId = 55 }, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        mockBlockRepo.Verify(b => b.DeleteAsync(existingBlock, It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHandler_DeberiaRetornar403_CuandoBloqueoPerteneceAOtroNegocio()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var blockFromOtherBusiness = new ScheduleBlock
        {
            Id = 77,
            BusinessId = otherBusinessId,
            StartDateTime = DateTime.UtcNow.AddHours(1),
            EndDateTime = DateTime.UtcNow.AddHours(2)
        };

        mockBlockRepo.Setup(b => b.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockFromOtherBusiness);

        var handler = new DeleteScheduleBlockHandler(
            mockBlockRepo.Object,
            mockUow.Object,
            mockCurrentUser.Object);

        // Act
        var result = await handler.Handle(new DeleteScheduleBlockCommand { BlockId = 77 }, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(Slotify.Application.Common.Models.ErrorType.Forbidden);
        mockBlockRepo.Verify(b => b.DeleteAsync(It.IsAny<ScheduleBlock>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetScheduleBlocksHandler_DeberiaRetornarListaDeBloqueos()
    {
        // Arrange
        var mockBlockRepo = new Mock<IScheduleBlockRepository>();
        var mockCurrentUser = new Mock<ICurrentUserService>();

        var businessId = Guid.NewGuid();
        mockCurrentUser.Setup(c => c.BusinessId).Returns(businessId);

        var start = DateTime.UtcNow;
        var end = start.AddDays(7);

        var blocksList = new List<ScheduleBlock>
        {
            new() { Id = 1, BusinessId = businessId, StartDateTime = start.AddHours(2), EndDateTime = start.AddHours(3), Reason = "Bloqueo 1" },
            new() { Id = 2, BusinessId = businessId, StartDateTime = start.AddHours(5), EndDateTime = start.AddHours(6), Reason = "Bloqueo 2" }
        };

        mockBlockRepo.Setup(b => b.GetByDateRangeAsync(businessId, start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocksList);

        var handler = new GetScheduleBlocksHandler(mockBlockRepo.Object, mockCurrentUser.Object);

        // Act
        var result = await handler.Handle(new GetScheduleBlocksQuery { StartDate = start, EndDate = end }, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data![0].Id.Should().Be(1);
        result.Data![1].Id.Should().Be(2);
    }
}
