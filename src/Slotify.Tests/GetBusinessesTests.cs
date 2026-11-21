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
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
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
            PrimaryColor = "#1E293B",
            IsDefault = false
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
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result.First();
        item.Id.Should().Be(businessId.ToString());
        item.Name.Should().Be("Barber Shop Elite");
        item.Slug.Should().Be("barber-shop-elite");
        item.Phone.Should().Be("+52 442 123 4567");
        item.Category.Should().Be("Barbería y Belleza");
        item.CategoryKey.Should().Be("barberia-y-belleza");
        item.MinAdvanceHours.Should().Be(3);
        item.MaxAdvanceDays.Should().Be(15);
        item.SectorTemplateId.Should().Be(3);
        item.LogoUrl.Should().Be("https://cdn.slotify.com/logos/barber.png");
        item.BrandColor.Should().Be("#1E293B");

        // Solo el servicio activo y no eliminado debe estar mapeado
        item.Services.Should().HaveCount(1);
        item.Services.First().Name.Should().Be("Corte Clásico");
        item.Services.First().Price.Should().Be(250m);
        item.Services.First().DurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task Handle_DeberiaUsarIdentidadDeCustomFormConfig_CuandoBrandNoTieneLogoNiColorPersonalizado()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Spa Zen",
            Slug = "spa-zen",
            CustomFormConfig = """
            {
              "logoUrl": "https://cdn.slotify.app/logos/spa-zen.png",
              "accentColor": "#10B981",
              "secondaryColor": "#059669",
              "fields": []
            }
            """
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].LogoUrl.Should().Be("https://cdn.slotify.app/logos/spa-zen.png");
        result[0].BrandColor.Should().Be("#10B981");
    }

    [Fact]
    public async Task Handle_NegocioSinSectorTemplate_DeberiaMapearCategoriaGeneralYKeyGeneral()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Servicio Independiente",
            Slug = "servicio-independiente",
            SectorTemplate = null,
            SectorTemplateId = null
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.Category.Should().Be("Servicios Generales");
        item.CategoryKey.Should().Be("general");
        item.SectorTemplateId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NegocioSinBrandNiCustomConfig_DeberiaRetornarValoresNulosParaLogoYColor()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Simple",
            Slug = "negocio-simple",
            Brand = null,
            CustomFormConfig = null
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].LogoUrl.Should().BeNull();
        result[0].BrandColor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NegocioConCustomFormConfigCorrupto_DeberiaManejarloSinLanzarExcepcion()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Config Rota",
            Slug = "negocio-config-rota",
            CustomFormConfig = "{ JSON_CORRUPTO_TOTALMENTE"
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].LogoUrl.Should().BeNull();
        result[0].BrandColor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BrandConColorValido_TienePrecedenciaSobreCustomFormConfig()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Estudio A",
            Slug = "estudio-a",
            Brand = new BusinessBrand
            {
                BusinessId = businessId,
                LogoUrl = "https://cdn.slotify.app/logos/brand-logo.png",
                PrimaryColor = "#112233",
                IsDefault = false
            },
            CustomFormConfig = """
            {
              "logoUrl": "https://cdn.slotify.app/logos/custom-logo.png",
              "accentColor": "#998877"
            }
            """
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].LogoUrl.Should().Be("https://cdn.slotify.app/logos/brand-logo.png");
        result[0].BrandColor.Should().Be("#112233");
    }

    [Fact]
    public async Task Handle_BrandConColorDefault_UsaColorDeCustomFormConfig()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Estudio B",
            Slug = "estudio-b",
            Brand = new BusinessBrand
            {
                BusinessId = businessId,
                PrimaryColor = "#000000",
                IsDefault = true
            },
            CustomFormConfig = """
            {
              "accentColor": "#3B82F6"
            }
            """
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].BrandColor.Should().Be("#3B82F6");
    }

    [Fact]
    public async Task Handle_NegocioSinSlug_GeneraSlugAutomaticoApartirDelNombre()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Consultorio San Lucas",
            Slug = string.Empty
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Slug.Should().Be("consultorio-san-lucas");
    }

    [Fact]
    public async Task Handle_NegocioConColeccionServiciosNula_RetornaListaVacia()
    {
        // Arrange
        var mockRepo = new Mock<IBusinessRepository>();
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Sin Coleccion",
            Slug = "negocio-sin-coleccion",
            Services = null!
        };

        mockRepo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([business]);

        var handler = new GetBusinessesHandler(mockRepo.Object);

        // Act
        var result = (await handler.Handle(new GetBusinessesQuery(), CancellationToken.None)).Data!.ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Services.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData("Barbería y Belleza", "barberia-y-belleza")]
    [InlineData("  Salud & Bienestar!  ", "salud-bienestar")]
    [InlineData("Clínica Médica / Estética", "clinica-medica-estetica")]
    [InlineData("Spa, Masajes y Relajación (VIP)", "spa-masajes-y-relajacion-vip")]
    [InlineData("100% Fitness & Gym", "100-fitness-gym")]
    [InlineData("CAFÉ & BISTRO", "cafe-bistro")]
    [InlineData("Diseño de Uñas y Pestañas", "diseno-de-unas-y-pestanas")]
    [InlineData("", "general")]
    [InlineData("   ", "general")]
    [InlineData(null, "general")]
    [InlineData("???", "general")]
    [InlineData("***---***", "general")]
    public void GenerateCategoryKey_DeberiaNormalizarEspaciosMayusculasAcentosYCaracteresEspeciales(
        string? input,
        string expected)
    {
        // Act
        var result = GetBusinessesHandler.GenerateCategoryKey(input);

        // Assert
        result.Should().Be(expected);
    }
}
