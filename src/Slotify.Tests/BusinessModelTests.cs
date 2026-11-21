namespace Slotify.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Xunit;

public class BusinessModelTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Business_ValoresPredeterminados_PropiedadesDeUbicacionSonOpcionales()
    {
        // Act
        var business = new Business
        {
            Name = "Establecimiento Modelo",
            Slug = "establecimiento-modelo"
        };

        // Assert
        business.Description.Should().BeNull();
        business.Street.Should().BeNull();
        business.ExteriorNumber.Should().BeNull();
        business.InteriorNumber.Should().BeNull();
        business.Neighborhood.Should().BeNull();
        business.City.Should().BeNull();
        business.State.Should().BeNull();
        business.PostalCode.Should().BeNull();
        business.CountryCode.Should().Be("MX", "el código de país predeterminado debe ser MX");
    }

    [Fact]
    public void BusinessConfiguration_MapeaColumnasDeUbicacionYDescripcionCorrectamente()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var model = context.Model;

        // Act
        var entityType = model.FindEntityType(typeof(Business));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("negocios");

        // calle
        var streetProp = entityType.FindProperty(nameof(Business.Street));
        streetProp.Should().NotBeNull();
        streetProp!.GetColumnName().Should().Be("calle");
        streetProp.GetMaxLength().Should().Be(150);
        streetProp.IsNullable.Should().BeTrue();

        // numero_exterior
        var extNumProp = entityType.FindProperty(nameof(Business.ExteriorNumber));
        extNumProp.Should().NotBeNull();
        extNumProp!.GetColumnName().Should().Be("numero_exterior");
        extNumProp.GetMaxLength().Should().Be(20);
        extNumProp.IsNullable.Should().BeTrue();

        // numero_interior
        var intNumProp = entityType.FindProperty(nameof(Business.InteriorNumber));
        intNumProp.Should().NotBeNull();
        intNumProp!.GetColumnName().Should().Be("numero_interior");
        intNumProp.GetMaxLength().Should().Be(20);
        intNumProp.IsNullable.Should().BeTrue();

        // colonia
        var neighProp = entityType.FindProperty(nameof(Business.Neighborhood));
        neighProp.Should().NotBeNull();
        neighProp!.GetColumnName().Should().Be("colonia");
        neighProp.GetMaxLength().Should().Be(100);
        neighProp.IsNullable.Should().BeTrue();

        // ciudad
        var cityProp = entityType.FindProperty(nameof(Business.City));
        cityProp.Should().NotBeNull();
        cityProp!.GetColumnName().Should().Be("ciudad");
        cityProp.GetMaxLength().Should().Be(100);
        cityProp.IsNullable.Should().BeTrue();

        // estado
        var stateProp = entityType.FindProperty(nameof(Business.State));
        stateProp.Should().NotBeNull();
        stateProp!.GetColumnName().Should().Be("estado");
        stateProp.GetMaxLength().Should().Be(100);
        stateProp.IsNullable.Should().BeTrue();

        // codigo_postal
        var zipProp = entityType.FindProperty(nameof(Business.PostalCode));
        zipProp.Should().NotBeNull();
        zipProp!.GetColumnName().Should().Be("codigo_postal");
        zipProp.GetMaxLength().Should().Be(10);
        zipProp.IsNullable.Should().BeTrue();

        // codigo_pais
        var countryProp = entityType.FindProperty(nameof(Business.CountryCode));
        countryProp.Should().NotBeNull();
        countryProp!.GetColumnName().Should().Be("codigo_pais");
        countryProp.GetMaxLength().Should().Be(2);
        countryProp.IsNullable.Should().BeTrue();
        countryProp.GetDefaultValue().Should().Be("MX");

        // descripcion
        var descProp = entityType.FindProperty(nameof(Business.Description));
        descProp.Should().NotBeNull();
        descProp!.GetColumnName().Should().Be("descripcion");
        descProp.GetMaxLength().Should().Be(500);
        descProp.IsNullable.Should().BeTrue();
    }

    [Fact]
    public async Task BusinessRepository_PersisteYRecuperaNegocioConUbicacionCompleta()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new BusinessRepository(context);

        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Barbería Don Juan Sucursal Centro",
            Slug = "barberia-don-juan-centro",
            Phone = "+525512345678",
            Description = "Barbería clásica con servicio de toalla caliente y café de especialidad.",
            Street = "Av. Insurgentes Sur",
            ExteriorNumber = "1602",
            InteriorNumber = "Piso 4, Local 402",
            Neighborhood = "Crédito Constructor",
            City = "Ciudad de México",
            State = "CDMX",
            PostalCode = "03940",
            CountryCode = "MX",
            OnboardingCompletedAt = DateTime.UtcNow
        };

        // Act
        await repository.AddAsync(business);
        await context.SaveChangesAsync();

        var retrieved = await repository.GetByIdAsync(businessId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Barbería Don Juan Sucursal Centro");
        retrieved.Description.Should().Be("Barbería clásica con servicio de toalla caliente y café de especialidad.");
        retrieved.Street.Should().Be("Av. Insurgentes Sur");
        retrieved.ExteriorNumber.Should().Be("1602");
        retrieved.InteriorNumber.Should().Be("Piso 4, Local 402");
        retrieved.Neighborhood.Should().Be("Crédito Constructor");
        retrieved.City.Should().Be("Ciudad de México");
        retrieved.State.Should().Be("CDMX");
        retrieved.PostalCode.Should().Be("03940");
        retrieved.CountryCode.Should().Be("MX");
    }

    [Fact]
    public async Task BusinessRepository_NegociosExistentesSinUbicacion_PuedenConsultarseYActualizarse()
    {
        // Arrange - Simulamos un negocio existente con los campos de ubicación en NULL (como los actuales de PostgreSQL)
        await using var context = CreateInMemoryDbContext();
        var repository = new BusinessRepository(context);

        var existingBusinessId = Guid.NewGuid();
        var legacyBusiness = new Business
        {
            Id = existingBusinessId,
            Name = "Negocio Antiguo Sin Dirección",
            Slug = "negocio-antiguo",
            Phone = "+525500000000",
            Description = null,
            Street = null,
            ExteriorNumber = null,
            InteriorNumber = null,
            Neighborhood = null,
            City = null,
            State = null,
            PostalCode = null,
            CountryCode = "MX",
            OnboardingCompletedAt = DateTime.UtcNow
        };

        await repository.AddAsync(legacyBusiness);
        await context.SaveChangesAsync();

        // Act 1: Consulta del negocio legado
        var fetched = await repository.GetByIdAsync(existingBusinessId);
        fetched.Should().NotBeNull();
        fetched!.Street.Should().BeNull();
        fetched.Description.Should().BeNull();

        // Act 2: Actualización agregando datos de ubicación física
        fetched.Street = "Calle Reforma";
        fetched.ExteriorNumber = "222";
        fetched.City = "Monterrey";
        fetched.State = "Nuevo León";
        fetched.PostalCode = "64000";
        fetched.Description = "Nueva descripción agregada posteriormente.";

        await repository.UpdateAsync(fetched);
        await context.SaveChangesAsync();

        // Assert: Recuperación tras actualización
        var updated = await repository.GetByIdAsync(existingBusinessId);
        updated.Should().NotBeNull();
        updated!.Street.Should().Be("Calle Reforma");
        updated.ExteriorNumber.Should().Be("222");
        updated.City.Should().Be("Monterrey");
        updated.State.Should().Be("Nuevo León");
        updated.PostalCode.Should().Be("64000");
        updated.Description.Should().Be("Nueva descripción agregada posteriormente.");
    }

    [Theory]
    [InlineData("Barbería Royal", "barbería-royal")]
    [InlineData("Studio 42", "studio-42")]
    [InlineData("  Clínica Central  ", "clínica-central")]
    [InlineData("SALÓN SPA", "salón-spa")]
    [InlineData("Taller-Mecánico", "taller-mecánico")]
    public void GenerateSlug_NombresNormales_GeneraSlugEsperado(string name, string expected)
    {
        var slug = Business.GenerateSlug(name);
        slug.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateSlug_NombreVacioONull_RetornaStringVacio(string? name)
    {
        var slug = Business.GenerateSlug(name);
        slug.Should().BeEmpty();
    }

    [Theory]
    [InlineData("me")]
    [InlineData("ME")]
    [InlineData("Me")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("AdMiN")]
    [InlineData("api")]
    [InlineData("book")]
    [InlineData("public")]
    [InlineData("settings")]
    [InlineData("auth")]
    [InlineData("  me  ")]
    public void IsReservedSlug_SegmentosReservadosYVariacionesDeMayusculas_RetornaTrue(string slug)
    {
        Business.IsReservedSlug(slug).Should().BeTrue();
        Business.IsValidSlug(slug).Should().BeFalse();
    }

    [Theory]
    [InlineData("mi-negocio")]
    [InlineData("barberia-royal")]
    [InlineData("mesa")]
    [InlineData("menu")]
    [InlineData("admin-salon")]
    [InlineData("booking-center")]
    [InlineData("apicius")]
    public void IsReservedSlug_NombresNoReservados_RetornaFalse(string slug)
    {
        Business.IsReservedSlug(slug).Should().BeFalse();
        Business.IsValidSlug(slug).Should().BeTrue();
    }
}
