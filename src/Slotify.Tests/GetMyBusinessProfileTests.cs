namespace Slotify.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Xunit;

public class GetMyBusinessProfileTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_PropietarioAutenticadoConNegocio_RetornaPerfilCompletoConUbicacionYDescripcion()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);

        var ownerId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            Name = "Barbería Don Juan Sucursal Polanco",
            Slug = "barberia-don-juan-polanco",
            Phone = "+525598765432",
            Description = "Barbería premium y cuidado masculino tradicional.",
            Street = "Av. Horacio",
            ExteriorNumber = "124",
            InteriorNumber = "Local B",
            Neighborhood = "Polanco V Sección",
            City = "Ciudad de México",
            State = "CDMX",
            PostalCode = "11560",
            CountryCode = "MX",
            TimeZone = "America/Mexico_City",
            MinAdvanceHours = 3,
            MaxAdvanceDays = 45,
            SectorTemplateId = 2,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var ownerUser = new User
        {
            Id = ownerId,
            FirstName = "Juan",
            LastName = "Propietario",
            Email = "juan@donjuan.com",
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = business
        };

        var membership = new UserBusiness
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            BusinessId = businessId,
            Role = UserRole.Owner,
            IsActive = true,
            User = ownerUser,
            Business = business
        };

        context.Businesses.Add(business);
        context.Users.Add(ownerUser);
        context.UserBusinesses.Add(membership);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new GetMyBusinessProfileHandler(userRepo, businessRepo, currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetMyBusinessProfileQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var dto = result.Data!;
        dto.Id.Should().Be(businessId);
        dto.Name.Should().Be("Barbería Don Juan Sucursal Polanco");
        dto.Slug.Should().Be("barberia-don-juan-polanco");
        dto.Phone.Should().Be("+525598765432");
        dto.Description.Should().Be("Barbería premium y cuidado masculino tradicional.");
        dto.Street.Should().Be("Av. Horacio");
        dto.ExteriorNumber.Should().Be("124");
        dto.InteriorNumber.Should().Be("Local B");
        dto.Neighborhood.Should().Be("Polanco V Sección");
        dto.City.Should().Be("Ciudad de México");
        dto.State.Should().Be("CDMX");
        dto.PostalCode.Should().Be("11560");
        dto.CountryCode.Should().Be("MX");
        dto.TimeZone.Should().Be("America/Mexico_City");
        dto.MinAdvanceHours.Should().Be(3);
        dto.MaxAdvanceDays.Should().Be(45);
        dto.SectorTemplateId.Should().Be(2);
        dto.OnboardingCompletedAt.Should().Be(business.OnboardingCompletedAt);
        dto.CreatedAt.Should().Be(business.CreatedAt);
    }

    [Fact]
    public async Task Handle_UsuarioSinNegocioAsociado_RetornaNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);

        var ownerId = Guid.NewGuid();

        // Usuario registrado como Owner pero sin negocio asociado en base de datos
        var orphanOwner = new User
        {
            Id = ownerId,
            FirstName = "Carlos",
            LastName = "Huérfano",
            Email = "carlos@huerfano.com",
            Role = UserRole.Owner,
            BusinessId = Guid.Empty
        };

        context.Users.Add(orphanOwner);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new GetMyBusinessProfileHandler(userRepo, businessRepo, currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetMyBusinessProfileQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Errors.Should().Contain("No existe un negocio asociado al propietario autenticado.");
    }

    [Fact]
    public async Task Handle_UsuarioConRolEmpleadoEnToken_RetornaForbidden()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>();
        var businessRepoMock = new Mock<IBusinessRepository>();

        var employeeId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(employeeId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Employee"); // No es Owner

        var handler = new GetMyBusinessProfileHandler(userRepoMock.Object, businessRepoMock.Object, currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetMyBusinessProfileQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Errors.Should().Contain("Acceso denegado. Se requieren permisos de propietario para consultar este perfil.");

        businessRepoMock.Verify(b => b.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UsuarioNoAutenticado_RetornaForbidden()
    {
        // Arrange
        var userRepoMock = new Mock<IUserRepository>();
        var businessRepoMock = new Mock<IBusinessRepository>();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        var handler = new GetMyBusinessProfileHandler(userRepoMock.Object, businessRepoMock.Object, currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetMyBusinessProfileQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        result.Errors.Should().Contain("No se pudo determinar la identidad del usuario autenticado.");
    }

    [Fact]
    public async Task Handle_ConsultaEsDeSoloLectura_NoModificaDatosEnBaseDeDatos()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);

        var ownerId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            Name = "Negocio Inmutable",
            Slug = "negocio-inmutable",
            Description = "Descripción original",
            Street = "Calle Original"
        };

        var ownerUser = new User
        {
            Id = ownerId,
            FirstName = "Dueño",
            LastName = "Test",
            Email = "dueno@test.com",
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = business
        };

        context.Businesses.Add(business);
        context.Users.Add(ownerUser);
        await context.SaveChangesAsync();

        // Limpiar el change tracker para validar que la consulta no agrega entidades en seguimiento
        context.ChangeTracker.Clear();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new GetMyBusinessProfileHandler(userRepo, businessRepo, currentUserServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetMyBusinessProfileQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        // Comprobar que no hay entidades marcadas como Modified, Added o Deleted
        context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Should().BeEmpty("la consulta debe ser estrictamente de solo lectura");

        // Comprobar que los datos permanecen idénticos
        var persistedBusiness = await context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId);
        persistedBusiness!.Name.Should().Be("Negocio Inmutable");
        persistedBusiness.Description.Should().Be("Descripción original");
        persistedBusiness.Street.Should().Be("Calle Original");
    }

    [Fact]
    public void GetMyBusinessProfileQuery_NoAceptaParametrosDeCliente_PrevieneSuplantacionDeNegocio()
    {
        // Arrange & Act
        var queryType = typeof(GetMyBusinessProfileQuery);
        var properties = queryType.GetProperties();
        var constructors = queryType.GetConstructors();

        // Assert - La query no expone ninguna propiedad de entrada para que el cliente pase un ID
        properties.Should().BeEmpty("la consulta debe resolver el negocio exclusivamente de la sesión y no aceptar IDs del cliente");
        constructors.Should().ContainSingle().Which.GetParameters().Should().BeEmpty("el constructor no debe solicitar parámetros externos");
    }
}
