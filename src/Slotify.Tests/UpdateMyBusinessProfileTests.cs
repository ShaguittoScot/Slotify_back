namespace Slotify.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Xunit;

public class UpdateMyBusinessProfileTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesAndPersistsBusinessProfileAndReturnsDto()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var ownerId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var initialCreatedAt = DateTime.UtcNow.AddDays(-15);
        var initialUpdatedAt = DateTime.UtcNow.AddDays(-10);

        var business = new Business
        {
            Id = businessId,
            Name = "Barbería Don Juan Original",
            Slug = "barberia-don-juan-original",
            Phone = "+525511112222",
            Description = "Descripción original",
            Street = "Calle Vieja",
            ExteriorNumber = "100",
            InteriorNumber = "A",
            Neighborhood = "Centro",
            City = "CDMX",
            State = "Ciudad de México",
            PostalCode = "06000",
            CountryCode = "MX",
            TimeZone = "America/Mexico_City",
            MinAdvanceHours = 2,
            MaxAdvanceDays = 30,
            SectorTemplateId = 1,
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-14),
            CreatedAt = initialCreatedAt,
            UpdatedAt = initialUpdatedAt
        };

        var ownerUser = new User
        {
            Id = ownerId,
            FirstName = "Juan",
            LastName = "Dueño",
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
        var persistedCreatedAt = business.CreatedAt;
        context.ChangeTracker.Clear();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);

        var command = new UpdateMyBusinessProfileCommand(
            Name: "Barbería Don Juan Polanco Renombrada",
            Phone: "+525599887766",
            Description: "Nueva descripción comercial de la barbería.",
            Street: "Av. Horacio",
            ExteriorNumber: "500",
            InteriorNumber: "Piso 2",
            Neighborhood: "Polanco",
            City: "Miguel Hidalgo",
            State: "CDMX",
            PostalCode: "11550",
            CountryCode: "mx" // Debe normalizarse a MX
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var dto = result.Data!;
        dto.Id.Should().Be(businessId);
        dto.Name.Should().Be("Barbería Don Juan Polanco Renombrada");
        dto.Slug.Should().Be("barberia-don-juan-original", "el slug del sistema nunca debe modificarse");
        dto.Phone.Should().Be("+525599887766");
        dto.Description.Should().Be("Nueva descripción comercial de la barbería.");
        dto.Street.Should().Be("Av. Horacio");
        dto.ExteriorNumber.Should().Be("500");
        dto.InteriorNumber.Should().Be("Piso 2");
        dto.Neighborhood.Should().Be("Polanco");
        dto.City.Should().Be("Miguel Hidalgo");
        dto.State.Should().Be("CDMX");
        dto.PostalCode.Should().Be("11550");
        dto.CountryCode.Should().Be("MX", "el código de país debe normalizarse en mayúsculas");
        dto.SectorTemplateId.Should().Be(1);
        dto.CreatedAt.Should().Be(persistedCreatedAt);
        dto.UpdatedAt.Should().BeAfter(initialUpdatedAt);

        // Verificar persistencia en base de datos
        var dbBusiness = await context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId);
        dbBusiness.Should().NotBeNull();
        dbBusiness!.Name.Should().Be("Barbería Don Juan Polanco Renombrada");
        dbBusiness.Phone.Should().Be("+525599887766");
        dbBusiness.Description.Should().Be("Nueva descripción comercial de la barbería.");
        dbBusiness.Street.Should().Be("Av. Horacio");
        dbBusiness.ExteriorNumber.Should().Be("500");
        dbBusiness.InteriorNumber.Should().Be("Piso 2");
        dbBusiness.Neighborhood.Should().Be("Polanco");
        dbBusiness.City.Should().Be("Miguel Hidalgo");
        dbBusiness.State.Should().Be("CDMX");
        dbBusiness.PostalCode.Should().Be("11550");
        dbBusiness.CountryCode.Should().Be("MX");
        dbBusiness.Slug.Should().Be("barberia-don-juan-original");
    }

    [Fact]
    public async Task Handle_OptionalFieldsCanBeClearedToNull_PersistsNulls()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var ownerId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            Name = "Spa Relajación Total",
            Slug = "spa-relajacion-total",
            Phone = "+525512345678",
            Description = "Descripción previa",
            Street = "Reforma",
            ExteriorNumber = "222",
            InteriorNumber = "3A",
            Neighborhood = "Juárez",
            City = "Cuauhtémoc",
            State = "CDMX",
            PostalCode = "06600",
            CountryCode = "MX"
        };

        var ownerUser = new User
        {
            Id = ownerId,
            FirstName = "Laura",
            LastName = "Dueña",
            Email = "laura@spa.com",
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
        var persistedCreatedAt = business.CreatedAt;
        context.ChangeTracker.Clear();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);

        // Actualización limpiando todos los campos opcionales
        var command = new UpdateMyBusinessProfileCommand(
            Name: "Spa Relajación Total Actualizado",
            Phone: null,
            Description: null,
            Street: null,
            ExteriorNumber: null,
            InteriorNumber: null,
            Neighborhood: null,
            City: null,
            State: null,
            PostalCode: null,
            CountryCode: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Spa Relajación Total Actualizado");
        result.Data.Phone.Should().BeNull();
        result.Data.Description.Should().BeNull();
        result.Data.Street.Should().BeNull();
        result.Data.ExteriorNumber.Should().BeNull();
        result.Data.InteriorNumber.Should().BeNull();
        result.Data.Neighborhood.Should().BeNull();
        result.Data.City.Should().BeNull();
        result.Data.State.Should().BeNull();
        result.Data.PostalCode.Should().BeNull();
        result.Data.CountryCode.Should().BeNull();

        // Verificar en base de datos
        var dbBusiness = await context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId);
        dbBusiness!.Phone.Should().BeNull();
        dbBusiness.Description.Should().BeNull();
        dbBusiness.Street.Should().BeNull();
        dbBusiness.ExteriorNumber.Should().BeNull();
        dbBusiness.InteriorNumber.Should().BeNull();
        dbBusiness.Neighborhood.Should().BeNull();
        dbBusiness.City.Should().BeNull();
        dbBusiness.State.Should().BeNull();
        dbBusiness.PostalCode.Should().BeNull();
        dbBusiness.CountryCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProtectedFieldsCannotBeModified_PreservesSystemProperties()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var ownerId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var onboardingDate = DateTime.UtcNow.AddMonths(-1);

        var business = new Business
        {
            Id = businessId,
            Name = "Clínica Dental Sonrisas",
            Slug = "clinica-dental-sonrisas",
            TimeZone = "America/Tijuana",
            MinAdvanceHours = 4,
            MaxAdvanceDays = 60,
            SectorTemplateId = 3,
            CustomFormConfig = "{\"theme\":\"blue\"}",
            OnboardingCompletedAt = onboardingDate
        };

        var ownerUser = new User
        {
            Id = ownerId,
            FirstName = "Doctor",
            LastName = "Sonrisas",
            Email = "doc@sonrisas.com",
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
        var persistedCreatedAt = business.CreatedAt;
        context.ChangeTracker.Clear();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);

        var command = new UpdateMyBusinessProfileCommand(
            Name: "Clínica Dental Sonrisas Especializadas"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        var dbBusiness = await context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId);
        dbBusiness.Should().NotBeNull();
        dbBusiness!.Id.Should().Be(businessId);
        dbBusiness.Slug.Should().Be("clinica-dental-sonrisas");
        dbBusiness.TimeZone.Should().Be("America/Tijuana");
        dbBusiness.MinAdvanceHours.Should().Be(4);
        dbBusiness.MaxAdvanceDays.Should().Be(60);
        dbBusiness.SectorTemplateId.Should().Be(3);
        dbBusiness.CustomFormConfig.Should().Be("{\"theme\":\"blue\"}");
        dbBusiness.OnboardingCompletedAt.Should().Be(onboardingDate);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);
        var command = new UpdateMyBusinessProfileCommand("Nuevo Negocio");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_NonOwnerRole_ReturnsForbidden()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var employeeId = Guid.NewGuid();
        var employeeUser = new User
        {
            Id = employeeId,
            FirstName = "Carlos",
            LastName = "Empleado",
            Email = "carlos@empleado.com",
            Role = UserRole.Employee
        };
        context.Users.Add(employeeUser);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(employeeId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Employee");

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);
        var command = new UpdateMyBusinessProfileCommand("Intento Empleado");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_BusinessNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var userRepo = new UserRepository(context);
        var businessRepo = new BusinessRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var ownerWithoutBusinessId = Guid.NewGuid();
        var ownerUser = new User
        {
            Id = ownerWithoutBusinessId,
            FirstName = "Sin",
            LastName = "Negocio",
            Email = "sin@negocio.com",
            Role = UserRole.Owner,
            BusinessId = Guid.NewGuid()
        };
        context.Users.Add(ownerUser);
        await context.SaveChangesAsync();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserServiceMock.Setup(s => s.UserId).Returns(ownerWithoutBusinessId);
        currentUserServiceMock.Setup(s => s.Role).Returns("Owner");

        var handler = new UpdateMyBusinessProfileHandler(userRepo, businessRepo, unitOfWork, currentUserServiceMock.Object);
        var command = new UpdateMyBusinessProfileCommand("Negocio Inexistente");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData("", "El nombre del negocio es requerido.")]
    [InlineData("   ", "El nombre del negocio es requerido.")]
    public void Validator_EmptyName_FailsValidation(string invalidName, string expectedMessage)
    {
        // Arrange
        var validator = new UpdateMyBusinessProfileValidator();
        var command = new UpdateMyBusinessProfileCommand(invalidName);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains(expectedMessage));
    }

    [Fact]
    public void Validator_FieldsExceedingMaxLength_FailsValidation()
    {
        // Arrange
        var validator = new UpdateMyBusinessProfileValidator();
        var command = new UpdateMyBusinessProfileCommand(
            Name: new string('A', 151),
            Phone: new string('1', 25),
            Description: new string('D', 501),
            Street: new string('S', 151),
            ExteriorNumber: new string('E', 21),
            InteriorNumber: new string('I', 21),
            Neighborhood: new string('N', 101),
            City: new string('C', 101),
            State: new string('S', 101),
            PostalCode: new string('P', 11),
            CountryCode: "MEXICO"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
        result.Errors.Should().Contain(e => e.PropertyName == "Street");
        result.Errors.Should().Contain(e => e.PropertyName == "ExteriorNumber");
        result.Errors.Should().Contain(e => e.PropertyName == "InteriorNumber");
        result.Errors.Should().Contain(e => e.PropertyName == "Neighborhood");
        result.Errors.Should().Contain(e => e.PropertyName == "City");
        result.Errors.Should().Contain(e => e.PropertyName == "State");
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }

    [Theory]
    [InlineData("12")]
    [InlineData("M")]
    [InlineData("MEX")]
    public void Validator_InvalidCountryCode_FailsValidation(string invalidCountryCode)
    {
        // Arrange
        var validator = new UpdateMyBusinessProfileValidator();
        var command = new UpdateMyBusinessProfileCommand(
            Name: "Negocio Válido",
            CountryCode: invalidCountryCode
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CountryCode");
    }

    [Fact]
    public void Validator_ValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new UpdateMyBusinessProfileValidator();
        var command = new UpdateMyBusinessProfileCommand(
            Name: "Estudio de Yoga Serenity",
            Phone: "+52 55 1234 5678",
            Description: "Clases de yoga y meditación guiada.",
            Street: "Av. Chapultepec",
            ExteriorNumber: "450",
            InteriorNumber: "Depto 4",
            Neighborhood: "Roma Norte",
            City: "Cuauhtémoc",
            State: "Ciudad de México",
            PostalCode: "06700",
            CountryCode: "MX"
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
