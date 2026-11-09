using FluentAssertions;
using Moq;
using Slotify.Application.Features.Businesses.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class GetBusinessesTests
{
    [Fact]
    public async Task Handle_DeberiaRetornarListaVacia_CuandoNoExistenNegocios()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Business>());

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetBusinessesQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeberiaMapearCorrectamenteNegocioConMarcaYServicios()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var businessId = Guid.NewGuid();
        var template = new SectorTemplate { Id = 3, Name = "Barbería y Belleza" };
        var brand = new BusinessBrand
        {
            BusinessId = businessId,
            LogoUrl = "https://cdn.slotify.com/logos/barber.png",
            PrimaryColor = "#1E293B"
        };
        var activeService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Corte Clásico",
            Price = 250m,
            DurationMinutes = 45,
            IsActive = true,
            DeletedAt = null
        };
        var inactiveService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Servicio Inactivo",
            Price = 100m,
            DurationMinutes = 30,
            IsActive = false,
            DeletedAt = null
        };
        var deletedService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Servicio Eliminado",
            Price = 150m,
            DurationMinutes = 20,
            IsActive = true,
            DeletedAt = DateTime.UtcNow
        };

        var business = new Business
        {
            Id = businessId,
            Name = "Barber Shop Elite",
            Slug = "barber-shop-elite",
            Phone = "+52 442 123 4567",
            TimeZone = "America/Mexico_City",
            MinAdvanceHours = 3,
            MaxAdvanceDays = 15,
            SectorTemplateId = 3,
            SectorTemplate = template,
            Brand = brand,
            Services = new List<Service> { activeService, inactiveService, deletedService }
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Business> { business });

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result.First();
        item.Id.Should().Be(businessId.ToString());
        item.Name.Should().Be("Barber Shop Elite");
        item.Slug.Should().Be("barber-shop-elite");
        item.Phone.Should().Be("+52 442 123 4567");
        item.Category.Should().Be("Barbería y Belleza");
        item.CategoryKey.Should().Be("barbería y belleza");
        item.MinAdvanceHours.Should().Be(3);
        item.MaxAdvanceDays.Should().Be(15);
        item.LogoUrl.Should().Be("https://cdn.slotify.com/logos/barber.png");
        item.BrandColor.Should().Be("#1E293B");

        // Solo el servicio activo y no eliminado debe estar mapeado
        item.Services.Should().HaveCount(1);
        item.Services.First().Name.Should().Be("Corte Clásico");
        item.Services.First().Price.Should().Be(250m);
        item.Services.First().DurationMinutes.Should().Be(45);
    }
}

