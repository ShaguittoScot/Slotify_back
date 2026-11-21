using FluentAssertions;
using Moq;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class GetBusinessBySlugTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_SlugInvalidoOVacio_RetornaNotFound(string? slug)
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var handler = new GetBusinessBySlugHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessBySlugQuery(slug!), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        mockRepo.Verify(r => r.GetBySlugWithDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NegocioNoEncontradoORepositorioRetornaNull_RetornaNotFound()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        mockRepo.Setup(r => r.GetBySlugWithDetailsAsync("inexistente", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Business?)null);

        var handler = new GetBusinessBySlugHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessBySlugQuery("inexistente"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NegocioExistente_MapeaCamposYServiciosCorrectamente()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var businessId = Guid.NewGuid();
        var template = new SectorTemplate { Id = 2, Name = "Salud y Medicina" };
        var brand = new BusinessBrand
        {
            BusinessId = businessId,
            LogoUrl = "https://cdn.slotify.app/logos/clinica.png",
            PrimaryColor = "#2563EB",
            IsDefault = false
        };

        var service1 = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Consulta General",
            Price = 450m,
            DurationMinutes = 30,
            BufferTimeMinutes = 10,
            IsActive = true,
            DeletedAt = null
        };

        var business = new Business
        {
            Id = businessId,
            Name = "Clínica San Rafael",
            Slug = "clinica-san-rafael",
            Phone = "+52 55 1234 5678",
            TimeZone = "America/Mexico_City",
            MinAdvanceHours = 2,
            MaxAdvanceDays = 30,
            SectorTemplateId = 2,
            SectorTemplate = template,
            Brand = brand,
            OnboardingCompletedAt = DateTime.UtcNow,
            Services = new List<Service> { service1 }
        };

        mockRepo.Setup(r => r.GetBySlugWithDetailsAsync("clinica-san-rafael", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var handler = new GetBusinessBySlugHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessBySlugQuery("  Clinica-San-Rafael  "), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var dto = result.Data!;
        dto.Id.Should().Be(businessId.ToString());
        dto.Name.Should().Be("Clínica San Rafael");
        dto.Slug.Should().Be("clinica-san-rafael");
        dto.Phone.Should().Be("+52 55 1234 5678");
        dto.Category.Should().Be("Salud y Medicina");
        dto.CategoryKey.Should().Be("salud-y-medicina");
        dto.TimeZone.Should().Be("America/Mexico_City");
        dto.MinAdvanceHours.Should().Be(2);
        dto.MaxAdvanceDays.Should().Be(30);
        dto.SectorTemplateId.Should().Be(2);
        dto.LogoUrl.Should().Be("https://cdn.slotify.app/logos/clinica.png");
        dto.BrandColor.Should().Be("#2563EB");

        dto.Services.Should().ContainSingle();
        var sDto = dto.Services.First();
        sDto.Id.Should().Be(service1.Id);
        sDto.Name.Should().Be("Consulta General");
        sDto.Description.Should().BeNull();
        sDto.Price.Should().Be(450m);
        sDto.DurationMinutes.Should().Be(30);
        sDto.BufferTimeMinutes.Should().Be(10);
        sDto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FallbackDeLogoYColorDesdeCustomFormConfig_CuandoBrandNoLosDefine()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            Name = "Estudio Creativo",
            Slug = "estudio-creativo",
            CustomFormConfig = "{\"logoUrl\":\"https://fallback.com/logo.png\",\"accentColor\":\"#E11D48\"}",
            Brand = new BusinessBrand
            {
                BusinessId = businessId,
                LogoUrl = null,
                PrimaryColor = "#000000",
                IsDefault = true
            },
            OnboardingCompletedAt = DateTime.UtcNow
        };

        mockRepo.Setup(r => r.GetBySlugWithDetailsAsync("estudio-creativo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var handler = new GetBusinessBySlugHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessBySlugQuery("estudio-creativo"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data!.LogoUrl.Should().Be("https://fallback.com/logo.png");
        result.Data!.BrandColor.Should().Be("#E11D48");
        result.Data!.Category.Should().Be("Servicios Generales");
        result.Data!.CategoryKey.Should().Be("general");
    }
}
