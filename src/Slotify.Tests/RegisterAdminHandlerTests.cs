namespace Slotify.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.Commands;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Xunit;

public class RegisterAdminHandlerTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Mock<IJwtService> CreateJwtServiceMock()
    {
        var mock = new Mock<IJwtService>();
        mock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns("mock-access-token");
        mock.Setup(j => j.GenerateRefreshToken())
            .Returns("mock-refresh-token");
        return mock;
    }

    [Fact]
    public async Task Handle_RegistroValido_CreaNegocioUsuarioYMembresiaUserBusiness()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var jwtMock = CreateJwtServiceMock();

        var handler = new RegisterAdminHandler(userRepo, businessRepo, unitOfWork, jwtMock.Object);

        var userId = Guid.NewGuid();
        var command = new RegisterAdminCommand
        {
            Id = userId,
            FirstName = "Carlos",
            LastName = "Dueño",
            Email = "carlos.dueno@test.com",
            BusinessName = "Barbería Royal",
            BusinessPhone = "+525512345678"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Respuesta del handler
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
        result.Data.Email.Should().Be("carlos.dueno@test.com");
        result.Data.FirstName.Should().Be("Carlos");
        result.Data.LastName.Should().Be("Dueño");
        result.Data.FullName.Should().Be("Carlos Dueño");
        result.Data.Role.Should().Be("Owner");
        result.Data.IsOnboardingCompleted.Should().BeFalse();
        result.Data.AccessToken.Should().Be("mock-access-token");
        result.Data.RefreshToken.Should().Be("mock-refresh-token");

        // Assert - Persistencia del Negocio
        var createdBusiness = await context.Businesses.FirstOrDefaultAsync(b => b.Id == result.Data.BusinessId);
        createdBusiness.Should().NotBeNull();
        createdBusiness!.Name.Should().Be("Barbería Royal");
        createdBusiness.Slug.Should().Be("barbería-royal");
        createdBusiness.Phone.Should().Be("+525512345678");

        // Assert - Persistencia del Usuario y retrocompatibilidad 1:1
        var createdUser = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("carlos.dueno@test.com");
        createdUser.Role.Should().Be(UserRole.Owner);
        createdUser.BusinessId.Should().Be(createdBusiness.Id);

        // Assert - Persistencia de la Membresía UserBusiness
        var createdMembership = await context.UserBusinesses
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BusinessId == createdBusiness.Id);

        createdMembership.Should().NotBeNull();
        createdMembership!.Id.Should().NotBeEmpty();
        createdMembership.UserId.Should().Be(userId);
        createdMembership.BusinessId.Should().Be(createdBusiness.Id);
        createdMembership.Role.Should().Be(UserRole.Owner);
        createdMembership.IsActive.Should().BeTrue();
        createdMembership.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Assert - Navegaciones cruzadas en memoria
        createdUser.UserBusinesses.Should().Contain(createdMembership);
        createdBusiness.UserBusinesses.Should().Contain(createdMembership);
    }

    [Fact]
    public async Task Handle_MembresiaTieneRolDuenoYEstaActiva()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var jwtMock = CreateJwtServiceMock();

        var handler = new RegisterAdminHandler(userRepo, businessRepo, unitOfWork, jwtMock.Object);

        var userId = Guid.NewGuid();
        var command = new RegisterAdminCommand
        {
            Id = userId,
            FirstName = "Mariana",
            LastName = "Salazar",
            Email = "mariana@estetica.com",
            BusinessName = "Estética Elite"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var memberships = await context.UserBusinesses.ToListAsync();
        memberships.Should().HaveCount(1);

        var membership = memberships.First();
        membership.Role.Should().Be(UserRole.Owner, "el rol del creador del negocio debe ser siempre DUENO");
        membership.IsActive.Should().BeTrue("la membresía inicial creada al registro debe estar activa");
        membership.UserId.Should().Be(userId);
        membership.BusinessId.Should().Be(result.Data!.BusinessId);
    }

    [Fact]
    public async Task Handle_UserIdYBusinessIdDeMembresiaCoincidenConEntidadesCreadas()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var jwtMock = CreateJwtServiceMock();

        var handler = new RegisterAdminHandler(userRepo, businessRepo, unitOfWork, jwtMock.Object);

        var userId = Guid.NewGuid();
        var command = new RegisterAdminCommand
        {
            Id = userId,
            FirstName = "Roberto",
            LastName = "Perez",
            Email = "roberto@clinica.com",
            BusinessName = "Clínica Dental Dentalis"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var user = await context.Users.SingleAsync();
        var business = await context.Businesses.SingleAsync();
        var membership = await context.UserBusinesses.SingleAsync();

        membership.UserId.Should().Be(user.Id);
        membership.BusinessId.Should().Be(business.Id);
        user.BusinessId.Should().Be(business.Id, "la FK legacy usuarios.id_negocio debe coincidir con el negocio");
    }

    [Fact]
    public async Task Handle_SiFallaUnitOfWork_PropagaExcepcionGarantizandoAtomicidad()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>();
        var businessRepoMock = new Mock<IBusinessRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = CreateJwtServiceMock();

        userRepoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        businessRepoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Simulamos fallo en la persistencia transaccional (ej. fallo de conexión o error de DB al guardar)
        unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Error simulado al guardar membresía en base de datos"));

        var handler = new RegisterAdminHandler(userRepoMock.Object, businessRepoMock.Object, unitOfWorkMock.Object, jwtMock.Object);

        var command = new RegisterAdminCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "Lucia",
            LastName = "Mendez",
            Email = "lucia@test.com",
            BusinessName = "Spa Serenidad"
        };

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithMessage("*Error simulado al guardar membresía en base de datos*");

        // Verificamos que los repositorios fueron llamados pero la transacción no se completó con éxito
        businessRepoMock.Verify(b => b.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepoMock.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        jwtMock.Verify(j => j.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailExistente_RetornaConflictYNoPersisteMembresia()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>();
        var businessRepoMock = new Mock<IBusinessRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = CreateJwtServiceMock();

        userRepoMock.Setup(r => r.EmailExistsAsync("existente@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterAdminHandler(userRepoMock.Object, businessRepoMock.Object, unitOfWorkMock.Object, jwtMock.Object);

        var command = new RegisterAdminCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Existente",
            Email = "existente@test.com",
            BusinessName = "Mi Negocio"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Errors.Should().Contain("El correo electrónico ya está registrado.");

        businessRepoMock.Verify(b => b.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepoMock.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SlugExistente_RetornaConflictYNoPersisteMembresia()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>();
        var businessRepoMock = new Mock<IBusinessRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var jwtMock = CreateJwtServiceMock();

        userRepoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        businessRepoMock.Setup(r => r.SlugExistsAsync("negocio-duplicado", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RegisterAdminHandler(userRepoMock.Object, businessRepoMock.Object, unitOfWorkMock.Object, jwtMock.Object);

        var command = new RegisterAdminCommand
        {
            Id = Guid.NewGuid(),
            FirstName = "Pedro",
            LastName = "Gomez",
            Email = "pedro@test.com",
            BusinessName = "Negocio Duplicado"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Errors.Should().Contain("El nombre del negocio ya está en uso.");

        businessRepoMock.Verify(b => b.AddAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepoMock.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NombreCompletoEnFullName_SeparaCorrectamenteYGeneraMembresia()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var jwtMock = CreateJwtServiceMock();

        var handler = new RegisterAdminHandler(userRepo, businessRepo, unitOfWork, jwtMock.Object);

        var userId = Guid.NewGuid();
        var command = new RegisterAdminCommand
        {
            Id = userId,
            FirstName = "",
            LastName = "",
            FullName = "Andrea Morales",
            Email = "andrea@test.com",
            BusinessName = "Consultorio Andrea"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.FirstName.Should().Be("Andrea");
        result.Data.LastName.Should().Be("Morales");

        var membership = await context.UserBusinesses.FirstOrDefaultAsync(ub => ub.UserId == userId);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(UserRole.Owner);
        membership.IsActive.Should().BeTrue();
    }
}
