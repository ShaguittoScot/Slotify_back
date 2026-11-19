using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Application.Features.Onboarding.Commands;
using Slotify.Application.Features.Onboarding.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

/// <summary>
/// Pruebas de seguridad, autorización y aislamiento multi-inquilino para el flujo de onboarding B2B.
/// Contiene:
/// 1. Pruebas de integración HTTP (pipeline JwtBearer + Authorize + CurrentUserService).
/// 2. Pruebas unitarias del handler (CompleteBusinessOnboardingHandler).
/// </summary>
public class OnboardingSecurityTests
{
    private const string TestJwtSecret = "TestSecretKey_ForUS022_AtLeast32CharsLong!!";

    #region Helpers de Integración

    private static WebApplicationFactory<Program> CreateTestFactory(
        Mock<IBusinessOnboardingPersistence>? persistenceMock = null,
        Mock<IBusinessRepository>? bizRepoMock = null,
        Mock<ISectorTemplateRepository>? templateRepoMock = null)
    {
        persistenceMock ??= new Mock<IBusinessOnboardingPersistence>();
        bizRepoMock ??= new Mock<IBusinessRepository>();
        templateRepoMock ??= new Mock<ISectorTemplateRepository>();

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        // Por defecto, sector template 1 existe
        templateRepoMock
            .Setup(t => t.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SectorTemplate { Id = 1, Name = "Barbería" });

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = TestJwtSecret,
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=slotify_test;Username=postgres;Password=postgres"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IBusinessOnboardingPersistence>();
                    services.RemoveAll<IBusinessRepository>();
                    services.RemoveAll<ISectorTemplateRepository>();

                    services.AddScoped(_ => persistenceMock.Object);
                    services.AddScoped(_ => bizRepoMock.Object);
                    services.AddScoped(_ => templateRepoMock.Object);
                });
            });
    }

    private static string GenerateToken(
        Guid userId,
        Guid? businessId = null,
        string role = "Owner",
        DateTime? expiresUtc = null,
        bool includeSub = true)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestJwtSecret);
        var now = DateTime.UtcNow;
        var expires = expiresUtc ?? now.AddMinutes(30);
        var notBefore = expires <= now ? expires.AddMinutes(-10) : now.AddMinutes(-1);

        var claims = new List<Claim>();

        if (includeSub)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
        }

        claims.Add(new Claim(JwtRegisteredClaimNames.Email, "owner@slotify.com"));
        claims.Add(new Claim(ClaimTypes.Role, role));

        if (businessId.HasValue)
        {
            claims.Add(new Claim("businessId", businessId.Value.ToString()));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = "Slotify",
            Audience = "Slotify.Api",
            NotBefore = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    private static object CreateValidPayload() => new
    {
        firstName = "Carlos",
        lastName = "Santana",
        business = new
        {
            name = "Barbería Santana",
            phone = "+525511223344",
            sectorTemplateId = 1,
            timeZone = "America/Mexico_City"
        },
        businessHours = new[]
        {
            new { dayOfWeek = 1, startTime = "09:00", endTime = "18:00" },
            new { dayOfWeek = 2, startTime = "09:00", endTime = "18:00" }
        },
        services = new[]
        {
            new { name = "Corte Clásico", durationMinutes = 45, price = 250.00m }
        }
    };

    #endregion

    #region 1. Pruebas de Integración HTTP (Autenticación, Autorización y Pipeline Real)

    [Fact]
    public async Task CompleteBusinessOnboarding_SinToken_Retorna401Unauthorized()
    {
        // ARRANGE
        using var factory = CreateTestFactory();
        using var client = factory.CreateClient();

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_ConTokenExpirado_Retorna401Unauthorized()
    {
        // ARRANGE
        using var factory = CreateTestFactory();
        using var client = factory.CreateClient();
        var expiredToken = GenerateToken(Guid.NewGuid(), Guid.NewGuid(), expiresUtc: DateTime.UtcNow.AddMinutes(-5));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_ConRolNoAutorizado_Retorna403Forbidden()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();

        // El usuario existe en DB pero tiene rol Employee
        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Role = UserRole.Employee,
                BusinessId = businessId,
                Business = new Business { Id = businessId, Name = "Test" }
            });

        using var factory = CreateTestFactory(persistenceMock: persistenceMock);
        using var client = factory.CreateClient();
        var employeeToken = GenerateToken(userId, businessId, role: "Employee");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_SinClaimBusinessIdEnToken_Retorna403Forbidden()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Role = UserRole.Owner,
                BusinessId = businessId,
                Business = new Business { Id = businessId, Name = "Test" }
            });

        using var factory = CreateTestFactory(persistenceMock: persistenceMock);
        using var client = factory.CreateClient();
        // Token SIN claim businessId
        var tokenWithoutBusiness = GenerateToken(userId, businessId: null, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenWithoutBusiness);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_ConTokenDeOtroNegocio_Retorna403Forbidden()
    {
        // ARRANGE: El usuario en DB pertenece a Negocio B, pero el token dice Negocio A
        var userId = Guid.NewGuid();
        var negocioA = Guid.NewGuid();
        var negocioB = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = userId,
                Role = UserRole.Owner,
                BusinessId = negocioB,
                Business = new Business { Id = negocioB, Name = "Negocio B" }
            });

        using var factory = CreateTestFactory(persistenceMock: persistenceMock);
        using var client = factory.CreateClient();
        // Token con negocioA != negocioB
        var tokenNegocioA = GenerateToken(userId, negocioA, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        persistenceMock.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_UsuarioNoExisteEnDB_Retorna404NotFound()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        using var factory = CreateTestFactory(persistenceMock: persistenceMock);
        using var client = factory.CreateClient();
        var token = GenerateToken(userId, businessId, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_UsuarioAutorizado_Retorna200Ok()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var bizRepoMock = new Mock<IBusinessRepository>();

        var testBusiness = new Business
        {
            Id = businessId,
            Name = "Negocio Original",
            Slug = "negocio-original",
            OnboardingCompletedAt = null
        };

        var testUser = new User
        {
            Id = userId,
            FirstName = "Carlos",
            LastName = "Santana",
            Email = "carlos@santana.com",
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = testBusiness
        };

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);

        persistenceMock
            .Setup(p => p.HasBusinessHoursAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        persistenceMock
            .Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        bizRepoMock
            .Setup(b => b.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        using var factory = CreateTestFactory(persistenceMock: persistenceMock, bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();
        var token = GenerateToken(userId, businessId, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstCompletionEnvelope = await response.Content.ReadFromJsonAsync<Result<AuthUserDto>>();
        firstCompletionEnvelope.Should().NotBeNull();
        firstCompletionEnvelope!.Data.Should().NotBeNull();
        firstCompletionEnvelope.Data!.IsOnboardingCompleted.Should().BeTrue();
        persistenceMock.Verify(p => p.AddBusinessBrand(It.IsAny<BusinessBrand>()), Times.Once);
        persistenceMock.Verify(p => p.AddBusinessHours(It.IsAny<IEnumerable<AvailabilitySchedule>>()), Times.Once);
        persistenceMock.Verify(p => p.AddServices(It.IsAny<IEnumerable<Service>>()), Times.Once);
        persistenceMock.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testBusiness.OnboardingCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_IdempotenteSiYaCompleto_Retorna200OkSinModificar()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var completionDate = DateTime.UtcNow.AddDays(-1);

        var testBusiness = new Business
        {
            Id = businessId,
            Name = "Negocio Ya Configurado",
            Slug = "negocio-ya-configurado",
            OnboardingCompletedAt = completionDate
        };

        var testUser = new User
        {
            Id = userId,
            FirstName = "Carlos",
            LastName = "Santana",
            Email = "carlos@santana.com",
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = testBusiness
        };

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        using var factory = CreateTestFactory(persistenceMock: persistenceMock);
        using var client = factory.CreateClient();
        var token = GenerateToken(userId, businessId, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ACT
        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            CreateValidPayload());

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var idempotentEnvelope = await response.Content.ReadFromJsonAsync<Result<AuthUserDto>>();
        idempotentEnvelope.Should().NotBeNull();
        idempotentEnvelope!.Data.Should().NotBeNull();
        idempotentEnvelope.Data!.IsOnboardingCompleted.Should().BeTrue();
        // No debe agregar brand, horarios ni servicios nuevamente
        persistenceMock.Verify(p => p.AddBusinessBrand(It.IsAny<BusinessBrand>()), Times.Never);
        persistenceMock.Verify(p => p.AddBusinessHours(It.IsAny<IEnumerable<AvailabilitySchedule>>()), Times.Never);
        persistenceMock.Verify(p => p.AddServices(It.IsAny<IEnumerable<Service>>()), Times.Never);
        persistenceMock.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteBusinessOnboarding_BodyConIdentificadoresSpoofeados_IgnoraBodyYOperaSobreToken()
    {
        // ARRANGE
        var legitimoUserId = Guid.NewGuid();
        var legitimoBusinessId = Guid.NewGuid();
        var victimaUserId = Guid.NewGuid();
        var victimaBusinessId = Guid.NewGuid();

        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var bizRepoMock = new Mock<IBusinessRepository>();

        var testBusiness = new Business
        {
            Id = legitimoBusinessId,
            Name = "Mi Negocio Real",
            Slug = "mi-negocio-real"
        };

        var testUser = new User
        {
            Id = legitimoUserId,
            FirstName = "Dueño Real",
            LastName = "Original",
            Email = "real@empresa.com",
            Role = UserRole.Owner,
            BusinessId = legitimoBusinessId,
            Business = testBusiness
        };

        // El mock solo responde para el usuario del token legítimo
        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(legitimoUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);

        persistenceMock
            .Setup(p => p.HasBusinessHoursAsync(legitimoBusinessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        persistenceMock
            .Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        bizRepoMock
            .Setup(b => b.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        using var factory = CreateTestFactory(persistenceMock: persistenceMock, bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();
        var token = GenerateToken(legitimoUserId, legitimoBusinessId, role: "Owner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // ACT: El atacante envía en el payload JSON campos "id" y "email" de la víctima
        var payloadSpoof = new
        {
            id = victimaUserId,
            email = "victima@otraempresa.com",
            firstName = "Atacante",
            lastName = "Modificador",
            business = new
            {
                name = "Nuevo Nombre",
                phone = "+525500000000",
                sectorTemplateId = 1,
                timeZone = "America/Mexico_City"
            },
            businessHours = new[]
            {
                new { dayOfWeek = 1, startTime = "09:00", endTime = "18:00" }
            },
            services = new[]
            {
                new { name = "Servicio", durationMinutes = 30, price = 100.00m }
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/onboarding/complete-business-onboarding",
            payloadSpoof);

        // ASSERT: Operación exitosa, pero NUNCA consultó ni mutó al usuario víctima
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        persistenceMock.Verify(p => p.GetUserWithBusinessDetailsAsync(victimaUserId, It.IsAny<CancellationToken>()), Times.Never);
        persistenceMock.Verify(p => p.GetUserWithBusinessDetailsAsync(legitimoUserId, It.IsAny<CancellationToken>()), Times.Once);
        testBusiness.Name.Should().Be("Nuevo Nombre");
    }

    #endregion

    #region 2. Pruebas Unitarias del Handler (CompleteBusinessOnboardingHandler)

    [Fact]
    public async Task Handler_UsuarioNoAutenticado_RetornaForbidden()
    {
        // ARRANGE
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var bizRepoMock = new Mock<IBusinessRepository>();
        var currentUserMock = new Mock<ICurrentUserService>();

        currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        currentUserMock.Setup(c => c.UserId).Returns((Guid?)null);

        var handler = new CompleteBusinessOnboardingHandler(
            persistenceMock.Object,
            bizRepoMock.Object,
            currentUserMock.Object);

        var command = new CompleteBusinessOnboardingCommand
        {
            FirstName = "Test",
            Business = new OnboardingBusinessDto
            {
                Name = "Test",
                SectorTemplateId = 1,
                TimeZone = "America/Mexico_City"
            },
            BusinessHours = [],
            Services = []
        };

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        persistenceMock.Verify(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handler_UsuarioConDatosParciales_RetornaFallo()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var bizRepoMock = new Mock<IBusinessRepository>();
        var currentUserMock = new Mock<ICurrentUserService>();

        currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        currentUserMock.Setup(c => c.UserId).Returns(userId);
        currentUserMock.Setup(c => c.BusinessId).Returns(businessId);
        currentUserMock.Setup(c => c.Role).Returns("Owner");

        var testBusiness = new Business
        {
            Id = businessId,
            Name = "Test Business",
            Brand = new BusinessBrand { BusinessId = businessId } // Ya tiene marca parcial
        };

        var testUser = new User
        {
            Id = userId,
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = testBusiness
        };

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);

        persistenceMock
            .Setup(p => p.HasBusinessHoursAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        var handler = new CompleteBusinessOnboardingHandler(
            persistenceMock.Object,
            bizRepoMock.Object,
            currentUserMock.Object);

        var command = new CompleteBusinessOnboardingCommand
        {
            FirstName = "Test",
            Business = new OnboardingBusinessDto
            {
                Name = "Test",
                SectorTemplateId = 1,
                TimeZone = "America/Mexico_City"
            },
            BusinessHours = [new OnboardingBusinessHourDto { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0) }],
            Services = [new OnboardingServiceDto { Name = "Svc", DurationMinutes = 30, Price = 100 }]
        };

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("datos parciales"));
        persistenceMock.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handler_SlugEnUsoPorOtroNegocio_RetornaConflict()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var persistenceMock = new Mock<IBusinessOnboardingPersistence>();
        var bizRepoMock = new Mock<IBusinessRepository>();
        var currentUserMock = new Mock<ICurrentUserService>();

        currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        currentUserMock.Setup(c => c.UserId).Returns(userId);
        currentUserMock.Setup(c => c.BusinessId).Returns(businessId);
        currentUserMock.Setup(c => c.Role).Returns("Owner");

        var testBusiness = new Business
        {
            Id = businessId,
            Name = "Nombre Inicial",
            Slug = "nombre-inicial"
        };

        var testUser = new User
        {
            Id = userId,
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = testBusiness
        };

        persistenceMock
            .Setup(p => p.GetUserWithBusinessDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);

        persistenceMock
            .Setup(p => p.HasBusinessHoursAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        persistenceMock
            .Setup(p => p.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        // El nuevo slug colisiona
        bizRepoMock
            .Setup(b => b.SlugExistsAsync("nombre-en-conflicto", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CompleteBusinessOnboardingHandler(
            persistenceMock.Object,
            bizRepoMock.Object,
            currentUserMock.Object);

        var command = new CompleteBusinessOnboardingCommand
        {
            FirstName = "Test",
            Business = new OnboardingBusinessDto
            {
                Name = "Nombre En Conflicto",
                SectorTemplateId = 1,
                TimeZone = "America/Mexico_City"
            },
            BusinessHours = [new OnboardingBusinessHourDto { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0) }],
            Services = [new OnboardingServiceDto { Name = "Svc", DurationMinutes = 30, Price = 100 }]
        };

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        persistenceMock.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
