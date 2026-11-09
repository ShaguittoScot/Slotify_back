using FluentAssertions;
using Moq;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Application.Features.Businesses.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class BusinessBrandingTests
{
    [Fact]
    public async Task UpdateBusinessBrandIdentity_ValidLogoAndCorporateColors_PersistsInFormConfig()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Barbería Capital",
            Slug = "barberia-capital"
        };

        var mockRepo = new Mock<IBusinessRepository>();
        var mockUow = new Mock<IUnitOfWork>();

        mockRepo.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var handler = new UpdateBusinessBrandIdentityCommandHandler(mockRepo.Object, mockUow.Object);
        var command = new UpdateBusinessBrandIdentityCommand(
            businessId,
            "https://cdn.slotify.app/logos/barberia-capital.png",
            "#e07a5f",
            "#c57747",
            "Barbería Capital VIP");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LogoUrl.Should().Be("https://cdn.slotify.app/logos/barberia-capital.png");
        result.Data.AccentColor.Should().Be("#E07A5F");
        result.Data.SecondaryColor.Should().Be("#C57747");
        result.Data.BusinessName.Should().Be("Barbería Capital VIP");

        mockRepo.Verify(r => r.UpdateAsync(business, It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBusinessBrandIdentity_InvalidHexColor_ReturnsFailure()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new UpdateBusinessBrandIdentityCommandHandler(mockRepo.Object, mockUow.Object);

        var command = new UpdateBusinessBrandIdentityCommand(
            Guid.NewGuid(),
            "https://cdn.slotify.app/logo.png",
            "NOT-A-HEX-COLOR",
            "#8B5CF6");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Business>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBusinessBookingConfig_WithCustomBranding_ReturnsLogoAndCorporateColors()
    {
        // Arrange
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Clínica Dental Sonrisas",
            Slug = "dental-sonrisas",
            CustomFormConfig = """
            {
              "title": "Reserva tu Consulta",
              "description": "Atención odontológica integral",
              "presentationStyle": "classic_scroll",
              "accentColor": "#10B981",
              "secondaryColor": "#0D9488",
              "logoUrl": "https://cdn.slotify.app/logos/dental.png",
              "businessName": "Clínica Dental Sonrisas",
              "fields": []
            }
            """
        };

        var mockBizRepo = new Mock<IBusinessRepository>();
        var mockTplRepo = new Mock<ISectorTemplateRepository>();

        mockBizRepo.Setup(r => r.GetBySlugAsync("dental-sonrisas", It.IsAny<CancellationToken>()))
            .ReturnsAsync(business);

        var handler = new GetBusinessBookingConfigQueryHandler(mockBizRepo.Object, mockTplRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessBookingConfigQuery("dental-sonrisas"), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LogoUrl.Should().Be("https://cdn.slotify.app/logos/dental.png");
        result.Data.AccentColor.Should().Be("#10B981");
        result.Data.SecondaryColor.Should().Be("#0D9488");
        result.Data.BusinessName.Should().Be("Clínica Dental Sonrisas");
    }
}

