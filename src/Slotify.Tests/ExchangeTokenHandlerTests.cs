using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Auth.Commands;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class ExchangeTokenHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IBusinessRepository> _bizRepoMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly ExchangeTokenHandler _handler;

    public ExchangeTokenHandlerTests()
    {
        _handler = new ExchangeTokenHandler(
            _userRepoMock.Object,
            _bizRepoMock.Object,
            _jwtServiceMock.Object
        );

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
            .Returns("slotify-mock-access-token");
        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("slotify-mock-refresh-token");
    }

    [Fact]
    public async Task Handle_NegocioConOnboardingCompletedAt_RetornaIsOnboardingCompletedTrue()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        const string email = "owner@slotify.com";

        var user = new User
        {
            Id = userId,
            FirstName = "Carlos",
            LastName = "Dueño",
            Email = email,
            Role = UserRole.Owner,
            BusinessId = businessId
        };

        var business = new Business
        {
            Id = businessId,
            Name = "Negocio Configurado",
            Slug = "negocio-configurado",
            OnboardingCompletedAt = DateTime.UtcNow
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _bizRepoMock
            .Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var command = new ExchangeTokenCommand(userId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("slotify-mock-access-token");
        result.Data.User.Should().NotBeNull();
        result.Data.User.BusinessId.Should().Be(businessId);
        result.Data.User.IsOnboardingCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NegocioConOnboardingCompletedAtNulo_RetornaIsOnboardingCompletedFalse()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        const string email = "pending@slotify.com";

        var user = new User
        {
            Id = userId,
            FirstName = "Pedro",
            LastName = "Pendiente",
            Email = email,
            Role = UserRole.Owner,
            BusinessId = businessId
        };

        var business = new Business
        {
            Id = businessId,
            Name = "Negocio Sin Terminar",
            Slug = "negocio-sin-terminar",
            OnboardingCompletedAt = null
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _bizRepoMock
            .Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var command = new ExchangeTokenCommand(userId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.User.BusinessId.Should().Be(businessId);
        result.Data.User.IsOnboardingCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NegocioInexistente_RetornaIsOnboardingCompletedFalse()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        const string email = "inconsistent@slotify.com";

        var user = new User
        {
            Id = userId,
            FirstName = "Inconsistente",
            LastName = "Test",
            Email = email,
            Role = UserRole.Owner,
            BusinessId = businessId
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Negocio no existe en el repositorio (retorna null)
        _bizRepoMock
            .Setup(b => b.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        var command = new ExchangeTokenCommand(userId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.User.BusinessId.Should().Be(businessId);
        result.Data.User.IsOnboardingCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UsuarioNoExiste_RetornaFallo()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        const string email = "desconocido@slotify.com";

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new ExchangeTokenCommand(userId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("El usuario aún no cuenta con un negocio sincronizado.");
    }

    [Fact]
    public async Task Handle_IdDiscrepanciaEntreSupabaseYPostgres_RetornaConflictoSinVincular()
    {
        // ARRANGE
        var supabaseUserId = Guid.NewGuid();
        var postgresUserId = Guid.NewGuid(); // Distinto ID
        const string email = "discrepancia@slotify.com";

        var user = new User
        {
            Id = postgresUserId,
            FirstName = "Carlos",
            LastName = "Discrepancia",
            Email = email,
            Role = UserRole.Owner,
            BusinessId = Guid.NewGuid()
        };

        // Búsqueda por email encuentra al usuario con postgresUserId
        _userRepoMock
            .Setup(r => r.GetByIdAsync(supabaseUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ExchangeTokenCommand(supabaseUserId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT: No debe mutar el UUID ni vincular automáticamente
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Discrepancia de identidad"));
    }

    [Fact]
    public async Task Handle_UsuarioSinNegocioAsignado_RetornaConflicto()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        const string email = "sinnegocio@slotify.com";

        var user = new User
        {
            Id = userId,
            FirstName = "Sin",
            LastName = "Negocio",
            Email = email,
            Role = UserRole.Owner,
            BusinessId = Guid.Empty
        };

        _userRepoMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new ExchangeTokenCommand(userId, email);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Inconsistencia de datos"));
    }
}
