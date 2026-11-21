namespace Slotify.Tests;

using FluentAssertions;
using Mapster;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.DTOs;
using Xunit;

public class MapsterProofOfConceptTests
{
    [Fact]
    public void Adapt_UpdateBusinessProfileRequest_To_UpdateMyBusinessProfileCommand_MapsAllFieldsCorrectly()
    {
        // Arrange
        var request = new UpdateBusinessProfileRequest
        {
            Name = "Barbería Mapster Pro",
            Phone = "+52 55 1234 5678",
            Description = "Establecimiento de prueba para PoC Mapster",
            Street = "Av. Insurgentes Sur",
            ExteriorNumber = "1602",
            InteriorNumber = "Piso 4",
            Neighborhood = "Crédito Constructor",
            City = "Benito Juárez",
            State = "Ciudad de México",
            PostalCode = "03940",
            CountryCode = "MX"
        };

        // Act - Mapeo directo mediante Mapster Adapt sin configuración previa
        var command = request.Adapt<UpdateMyBusinessProfileCommand>();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("Barbería Mapster Pro");
        command.Phone.Should().Be("+52 55 1234 5678");
        command.Description.Should().Be("Establecimiento de prueba para PoC Mapster");
        command.Street.Should().Be("Av. Insurgentes Sur");
        command.ExteriorNumber.Should().Be("1602");
        command.InteriorNumber.Should().Be("Piso 4");
        command.Neighborhood.Should().Be("Crédito Constructor");
        command.City.Should().Be("Benito Juárez");
        command.State.Should().Be("Ciudad de México");
        command.PostalCode.Should().Be("03940");
        command.CountryCode.Should().Be("MX");
    }

    [Fact]
    public void Adapt_UpdateBusinessProfileRequest_WithNullOptionalFields_MapsNullsCorrectly()
    {
        // Arrange - Solo campo requerido presente, todos los opcionales en null
        var request = new UpdateBusinessProfileRequest
        {
            Name = "Spa Minimalista",
            Phone = null,
            Description = null,
            Street = null,
            ExteriorNumber = null,
            InteriorNumber = null,
            Neighborhood = null,
            City = null,
            State = null,
            PostalCode = null,
            CountryCode = null
        };

        // Act
        var command = request.Adapt<UpdateMyBusinessProfileCommand>();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("Spa Minimalista");
        command.Phone.Should().BeNull();
        command.Description.Should().BeNull();
        command.Street.Should().BeNull();
        command.ExteriorNumber.Should().BeNull();
        command.InteriorNumber.Should().BeNull();
        command.Neighborhood.Should().BeNull();
        command.City.Should().BeNull();
        command.State.Should().BeNull();
        command.PostalCode.Should().BeNull();
        command.CountryCode.Should().BeNull();
    }

    [Fact]
    public void Adapt_WithExplicitIsolatedConfig_ProducesIdenticalResultWithoutPollutingGlobalState()
    {
        // Arrange - Configuración local aislada (sin alterar TypeAdapterConfig.GlobalSettings)
        var isolatedConfig = new TypeAdapterConfig();
        isolatedConfig.NewConfig<UpdateBusinessProfileRequest, UpdateMyBusinessProfileCommand>();

        var request = new UpdateBusinessProfileRequest
        {
            Name = "Consultorio Dental Aislado",
            Phone = "+52 442 987 6543",
            City = "Querétaro",
            State = "QRO",
            CountryCode = "MX"
        };

        // Act
        var command = request.Adapt<UpdateMyBusinessProfileCommand>(isolatedConfig);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("Consultorio Dental Aislado");
        command.Phone.Should().Be("+52 442 987 6543");
        command.City.Should().Be("Querétaro");
        command.State.Should().Be("QRO");
        command.CountryCode.Should().Be("MX");
        command.Description.Should().BeNull();
        command.Street.Should().BeNull();
    }
}
