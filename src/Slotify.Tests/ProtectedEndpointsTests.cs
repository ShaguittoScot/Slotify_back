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
using Slotify.Application.Features.Calendar;
using Slotify.API.Services;
using Slotify.Infrastructure.Services;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

/// <summary>
/// Pruebas de integración del pipeline HTTP real (JwtBearer + Authorize + CurrentUserService) para US-022.
/// Verifica:
/// - Petición sin token → 401 Unauthorized
/// - Petición con token expirado → 401 Unauthorized
/// - Petición con token de otro negocio → 403 Forbidden
/// - Endpoint público GET /api/businesses/{slug}/booking-config → AllowAnonymous (200 OK)
/// - Aislamiento estricto del BusinessId extraído del token
/// </summary>
public class ProtectedEndpointsTests
{
    private const string TestJwtSecret = "TestSecretKey_ForUS022_AtLeast32CharsLong!!";

    private static WebApplicationFactory<Program> CreateTestFactory(
        Mock<IAppointmentRepository>? apptRepoMock = null,
        Mock<IScheduleBlockRepository>? blockRepoMock = null,
        Mock<IBusinessRepository>? bizRepoMock = null,
        Mock<ISectorTemplateRepository>? templateRepoMock = null,
        Mock<IUnitOfWork>? uowMock = null)
    {
        apptRepoMock ??= new Mock<IAppointmentRepository>();
        blockRepoMock ??= new Mock<IScheduleBlockRepository>();
        bizRepoMock ??= new Mock<IBusinessRepository>();
        templateRepoMock ??= new Mock<ISectorTemplateRepository>();
        uowMock ??= new Mock<IUnitOfWork>();

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
                    services.RemoveAll<IAppointmentRepository>();
                    services.RemoveAll<IScheduleBlockRepository>();
                    services.RemoveAll<IBusinessRepository>();
                    services.RemoveAll<ISectorTemplateRepository>();
                    services.RemoveAll<IUnitOfWork>();

                    services.AddScoped(_ => apptRepoMock.Object);
                    services.AddScoped(_ => blockRepoMock.Object);
                    services.AddScoped(_ => bizRepoMock.Object);
                    services.AddScoped(_ => templateRepoMock.Object);
                    services.AddScoped(_ => uowMock.Object);
                    services.AddScoped<ICalendarDatabase, TestCalendarDatabase>();
                });
            });
    }

    private static string GenerateToken(Guid userId, Guid businessId, string role = "Owner", DateTime? expiresUtc = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestJwtSecret);
        var now = DateTime.UtcNow;
        var expires = expiresUtc ?? now.AddMinutes(30);
        var notBefore = expires <= now ? expires.AddMinutes(-10) : now.AddMinutes(-1);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = JwtService.Issuer,
            Audience = JwtService.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "owner@slotify.com"),
                new Claim(ClaimTypes.Role, role),
                new Claim("businessId", businessId.ToString())
            ]),
            NotBefore = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task Login_IgnoraLaIdentidadDelBodyYExigeSesionDeSupabase()
    {
        var verifiedUser = new User
        {
            Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Email = "verificado@example.invalid"
        };
        var newUser = new SupabaseIdentity(Guid.NewGuid(), "nuevo@example.invalid");
        var supabase = new Mock<ISupabaseIdentityVerifier>();
        supabase.Setup(v => v.VerifyAsync("sesion-verificada", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseIdentity(verifiedUser.Id, verifiedUser.Email));
        supabase.Setup(v => v.VerifyAsync("sesion-nueva", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);
        supabase.Setup(v => v.VerifyAsync("otra-identidad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseIdentity(Guid.NewGuid(), verifiedUser.Email));
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(verifiedUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(verifiedUser);
        users.Setup(r => r.GetByEmailAsync(verifiedUser.Email, It.IsAny<CancellationToken>())).ReturnsAsync(verifiedUser);
        var writes = new Mock<IUnitOfWork>();
        using var baseFactory = CreateTestFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddScoped(_ => supabase.Object);
            services.AddScoped(_ => users.Object);
            services.AddScoped(_ => writes.Object);
        }));
        using var client = factory.CreateClient();
        var forged = new { userId = Guid.NewGuid(), email = "otra-persona@example.invalid" };

        (await client.PostAsJsonAsync("/api/auth/login", forged)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sesion-verificada");
        var response = await client.PostAsJsonAsync("/api/auth/login", forged);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("data").GetProperty("accessToken").GetString());
        token.Subject.Should().Be(verifiedUser.Id.ToString());
        token.Claims.Single(c => c.Type == "businessId").Value.Should().Be(verifiedUser.BusinessId.ToString());

        var repeatedSetup = await client.PostAsJsonAsync("/api/auth/sync", new
        {
            id = verifiedUser.Id,
            email = verifiedUser.Email,
            firstName = "Titular",
            businessName = "Nombre que no debe reemplazar el actual"
        });
        repeatedSetup.StatusCode.Should().Be(HttpStatusCode.OK);
        var existing = await repeatedSetup.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        existing.GetProperty("data").GetProperty("businessId").GetGuid().Should().Be(verifiedUser.BusinessId);
        writes.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "sesion-nueva");
        var missing = await client.PostAsJsonAsync("/api/auth/login", forged);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var missingBody = await missing.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        missingBody.GetProperty("code").GetString().Should().Be("business_profile_missing");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "otra-identidad");
        (await client.PostAsJsonAsync("/api/auth/login", forged)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchCalendarAppointment_ReprogramaCompletaYRechazaOtroNegocio()
    {
        var business = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(), BusinessId = business, StartTime = start, EndTime = start.AddHours(1)
        };
        var appointments = new Mock<IAppointmentRepository>();
        var blocks = new Mock<IScheduleBlockRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        appointments.Setup(r => r.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        appointments.Setup(r => r.GetByDateRangeAsync(business, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { appointment });
        blocks.Setup(r => r.GetByDateRangeAsync(business, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ScheduleBlock>());
        using var factory = CreateTestFactory(appointments, blocks, uowMock: unitOfWork);
        using var client = factory.CreateClient();
        var path = $"/api/calendar/appointments/{appointment.Id}";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(Guid.NewGuid(), Guid.NewGuid()));

        (await client.PatchAsJsonAsync(path, new { status = "cancelled" })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(Guid.NewGuid(), business));
        var newStart = start.AddMinutes(30);
        (await client.PatchAsJsonAsync(path, new { startTime = newStart, endTime = newStart.AddHours(1) }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PatchAsJsonAsync(path, new { status = "completed" })).StatusCode.Should().Be(HttpStatusCode.OK);

        appointment.StartTime.Should().Be(newStart);
        appointment.Status.Should().Be(AppointmentStatus.Completed);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetCalendarSlots_SinToken_Retorna401Unauthorized()
    {
        // ARRANGE
        using var factory = CreateTestFactory();
        using var client = factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/calendar/slots?startDate=2026-10-01T00:00:00Z&endDate=2026-10-31T23:59:59Z");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostScheduleBlocks_SinToken_Retorna401Unauthorized()
    {
        // ARRANGE
        using var factory = CreateTestFactory();
        using var client = factory.CreateClient();

        // ACT
        var response = await client.PostAsJsonAsync("/api/schedule-blocks", new
        {
            startDateTime = DateTime.UtcNow.AddHours(1),
            endDateTime = DateTime.UtcNow.AddHours(2),
            reason = "Comida"
        });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCalendarSlots_ConTokenExpirado_Retorna401Unauthorized()
    {
        // ARRANGE
        using var factory = CreateTestFactory();
        using var client = factory.CreateClient();
        var expiredToken = GenerateToken(Guid.NewGuid(), Guid.NewGuid(), expiresUtc: DateTime.UtcNow.AddMinutes(-5));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // ACT
        var response = await client.GetAsync("/api/calendar/slots?startDate=2026-10-01T00:00:00Z&endDate=2026-10-31T23:59:59Z");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutBookingConfig_ConTokenDeOtroNegocio_Retorna403Forbidden()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var negocioB = Guid.NewGuid();
        var bizRepoMock = new Mock<IBusinessRepository>();

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.PutAsJsonAsync($"/api/businesses/{negocioB}/booking-config", new
        {
            formConfig = new { fields = Array.Empty<object>() }
        });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        bizRepoMock.Verify(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PutTemplate_ConTokenDeOtroNegocio_Retorna403Forbidden()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var negocioB = Guid.NewGuid();
        var bizRepoMock = new Mock<IBusinessRepository>();

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.PutAsJsonAsync($"/api/businesses/{negocioB}/template", new
        {
            newTemplateId = 2
        });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        bizRepoMock.Verify(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteScheduleBlock_DeOtroNegocio_Retorna403Forbidden()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var negocioB = Guid.NewGuid();
        var blockRepoMock = new Mock<IScheduleBlockRepository>();
        blockRepoMock
            .Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduleBlock
            {
                Id = 42,
                BusinessId = negocioB,
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Reason = "Bloqueo privado Negocio B"
            });

        using var factory = CreateTestFactory(blockRepoMock: blockRepoMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.DeleteAsync("/api/schedule-blocks/42");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        blockRepoMock.Verify(r => r.DeleteAsync(It.IsAny<ScheduleBlock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PutBookingConfig_ConTokenDelMismoNegocio_Retorna200Ok()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var bizRepoMock = new Mock<IBusinessRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        bizRepoMock
            .Setup(b => b.GetByIdAsync(negocioA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Business { Id = negocioA, Name = "Barbería A", Slug = "barberia-a" });
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock, uowMock: uowMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.PutAsJsonAsync($"/api/businesses/{negocioA}/booking-config", new
        {
            formConfig = new { presentationStyle = "single_page", fields = Array.Empty<object>() }
        });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bizRepoMock.Verify(b => b.UpdateAsync(It.Is<Business>(x => x.Id == negocioA), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBookingConfigBySlug_SinToken_EsPublicoYRetorna200Ok()
    {
        // ARRANGE
        var bizRepoMock = new Mock<IBusinessRepository>();
        bizRepoMock
            .Setup(b => b.GetBySlugAsync("barberia-publica", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Business
            {
                Id = Guid.NewGuid(),
                Name = "Barbería Pública",
                Slug = "barberia-publica",
                CustomFormConfig = """{"presentationStyle":"single_page","fields":[]}"""
            });

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();

        // ACT (sin header Authorization)
        var response = await client.GetAsync("/api/businesses/barberia-publica/booking-config");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBusinesses_SinToken_EsPublicoYRetorna200Ok()
    {
        // ARRANGE
        var bizRepoMock = new Mock<IBusinessRepository>();
        bizRepoMock
            .Setup(b => b.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Business { Id = Guid.NewGuid(), Name = "Barbería Pública", Slug = "barberia-publica" }]);

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();

        // ACT (sin header Authorization)
        var response = await client.GetAsync("/api/businesses");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutBranding_ConTokenDeOtroNegocio_Retorna403Forbidden()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var negocioB = Guid.NewGuid();
        var bizRepoMock = new Mock<IBusinessRepository>();

        using var factory = CreateTestFactory(bizRepoMock: bizRepoMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT
        var response = await client.PutAsJsonAsync($"/api/businesses/{negocioB}/branding", new
        {
            logoUrl = "https://cdn.slotify.app/logo.png",
            accentColor = "#6366F1"
        });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        bizRepoMock.Verify(b => b.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCalendarSlots_ConTokenValido_FiltraUnicamentePorElNegocioDelToken()
    {
        // ARRANGE
        var negocioA = Guid.NewGuid();
        var apptRepoMock = new Mock<IAppointmentRepository>();
        var blockRepoMock = new Mock<IScheduleBlockRepository>();

        apptRepoMock
            .Setup(r => r.GetByDateRangeAsync(negocioA, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Appointment
                {
                    Id = Guid.NewGuid(),
                    BusinessId = negocioA,
                    ClientName = "Cliente Exclusivo A",
                    StartTime = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2026, 10, 15, 11, 0, 0, DateTimeKind.Utc),
                    Status = AppointmentStatus.Confirmed,
                    AgreedTotal = 250m
                }
            ]);

        blockRepoMock
            .Setup(r => r.GetByDateRangeAsync(negocioA, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        using var factory = CreateTestFactory(apptRepoMock: apptRepoMock, blockRepoMock: blockRepoMock);
        using var client = factory.CreateClient();
        var tokenNegocioA = GenerateToken(Guid.NewGuid(), negocioA);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenNegocioA);

        // ACT — incluso si alguien intenta mandar ?businessId=otroGuid en la query string, se ignora y usa negocioA del token
        var otroNegocio = Guid.NewGuid();
        var response = await client.GetAsync(
            $"/api/calendar/slots?startDate=2026-10-01T00:00:00Z&endDate=2026-10-31T23:59:59Z&businessId={otroNegocio}");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Cliente Exclusivo A");
        apptRepoMock.Verify(
            r => r.GetByDateRangeAsync(negocioA, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
        apptRepoMock.Verify(
            r => r.GetByDateRangeAsync(otroNegocio, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

