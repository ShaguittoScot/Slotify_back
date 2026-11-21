using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Xunit;

namespace Slotify.Tests;

public class BusinessRepositoryTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_NegocioConOnboardingCompleto_EsRetornadoConSectorYMarca()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var sector = new SectorTemplate
        {
            Id = 1,
            Name = "Barbería y Estilismo"
        };

        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Barbería Don Juan",
            Slug = "barberia-don-juan",
            OnboardingCompletedAt = DateTime.UtcNow,
            SectorTemplate = sector,
            Brand = new BusinessBrand
            {
                BusinessId = businessId,
                LogoUrl = "https://cdn.slotify.app/logos/don-juan.png",
                PrimaryColor = "#1E293B",
                IsDefault = false
            }
        };

        context.SectorTemplates.Add(sector);
        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().ContainSingle();
        var item = result.First();
        item.Id.Should().Be(businessId);
        item.Name.Should().Be("Barbería Don Juan");
        item.SectorTemplate.Should().NotBeNull();
        item.SectorTemplate!.Name.Should().Be("Barbería y Estilismo");
        item.Brand.Should().NotBeNull();
        item.Brand!.LogoUrl.Should().Be("https://cdn.slotify.app/logos/don-juan.png");
        item.Brand.PrimaryColor.Should().Be("#1E293B");
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_NegociosConOnboardingIncompleto_SonExcluidos()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var pendingBusiness1 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Pendiente 1",
            Slug = "negocio-pendiente-1",
            OnboardingCompletedAt = null
        };

        var pendingBusiness2 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Pendiente 2",
            Slug = "negocio-pendiente-2",
            OnboardingCompletedAt = null
        };

        context.Businesses.AddRange(pendingBusiness1, pendingBusiness2);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_NegocioSinServicios_EsRetornadoConColeccionServiciosVacia()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var publishedBusiness = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Consultorio Nutricional",
            Slug = "consultorio-nutricional",
            OnboardingCompletedAt = DateTime.UtcNow,
            Services = []
        };

        context.Businesses.Add(publishedBusiness);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().ContainSingle();
        var item = result.First();
        item.Id.Should().Be(publishedBusiness.Id);
        item.Services.Should().NotBeNull();
        item.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_FiltraServicios_SoloRetornaActivosYNoEliminados()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var activeService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Corte clásico",
            Price = 150m,
            DurationMinutes = 30,
            IsActive = true,
            DeletedAt = null
        };

        var inactiveService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Servicio inactivo",
            Price = 100m,
            DurationMinutes = 30,
            IsActive = false,
            DeletedAt = null
        };

        var deletedService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Servicio eliminado",
            Price = 200m,
            DurationMinutes = 45,
            IsActive = true,
            DeletedAt = DateTime.UtcNow
        };

        var inactiveAndDeletedService = new Service
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Servicio inactivo y eliminado",
            Price = 250m,
            DurationMinutes = 60,
            IsActive = false,
            DeletedAt = DateTime.UtcNow
        };

        var publishedBusiness = new Business
        {
            Id = businessId,
            Name = "Barbería Premium",
            Slug = "barberia-premium",
            OnboardingCompletedAt = DateTime.UtcNow,
            Services =
            [
                activeService,
                inactiveService,
                deletedService,
                inactiveAndDeletedService
            ]
        };

        context.Businesses.Add(publishedBusiness);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().ContainSingle();
        var business = result.First();

        business.Services.Should().ContainSingle();
        business.Services.First().Id.Should().Be(activeService.Id);
        business.Services.First().Name.Should().Be("Corte clásico");
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_VariosNegociosConDistintosEstados_SoloRetornaAquellosCompletados()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var completedBusiness1 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Spa Relax",
            Slug = "spa-relax",
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-30),
            Services =
            [
                new Service
                {
                    Id = Guid.NewGuid(),
                    Name = "Masaje Sueco",
                    Price = 500m,
                    DurationMinutes = 60,
                    IsActive = true,
                    DeletedAt = null
                }
            ]
        };

        var completedBusiness2 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Gimnasio Fuerza",
            Slug = "gimnasio-fuerza",
            OnboardingCompletedAt = DateTime.UtcNow.AddHours(-2),
            Services = []
        };

        var pendingBusiness1 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Tatuajes Tinta",
            Slug = "tatuajes-tinta",
            OnboardingCompletedAt = null,
            Services =
            [
                new Service
                {
                    Id = Guid.NewGuid(),
                    Name = "Tatuaje Minimalista",
                    Price = 800m,
                    DurationMinutes = 90,
                    IsActive = true,
                    DeletedAt = null
                }
            ]
        };

        var pendingBusiness2 = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Estudio Fotográfico",
            Slug = "estudio-fotografico",
            OnboardingCompletedAt = null
        };

        context.Businesses.AddRange(
            completedBusiness1,
            completedBusiness2,
            pendingBusiness1,
            pendingBusiness2
        );
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(b => b.Id).Should().Contain([completedBusiness1.Id, completedBusiness2.Id]);
        result.Select(b => b.Id).Should().NotContain(pendingBusiness1.Id);
        result.Select(b => b.Id).Should().NotContain(pendingBusiness2.Id);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_UsaAsNoTracking_NoModificaNiRastreaEntidadesEnChangeTracker()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var businessId = Guid.NewGuid();
        var originalName = "Barbería Original";

        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Businesses.Add(new Business
            {
                Id = businessId,
                Name = originalName,
                Slug = "barberia-original",
                OnboardingCompletedAt = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert
        await using (var queryContext = new AppDbContext(options))
        {
            var repository = new BusinessRepository(queryContext);
            var result = await repository.GetAllWithDetailsAsync();

            result.Should().ContainSingle();

            // Verificar que ChangeTracker no rastrea las entidades gracias a AsNoTracking()
            queryContext.ChangeTracker.Entries<Business>().Should().BeEmpty();

            // Modificar propiedad en memoria
            result.First().Name = "Nombre Modificado En Memoria";

            // SaveChanges no debería tener cambios pendientes
            queryContext.ChangeTracker.HasChanges().Should().BeFalse();
            await queryContext.SaveChangesAsync();
        }

        // Verificar que en la base de datos la entidad permanezca intacta
        await using (var verifyContext = new AppDbContext(options))
        {
            var persistedBusiness = await verifyContext.Businesses.FindAsync(businessId);
            persistedBusiness.Should().NotBeNull();
            persistedBusiness!.Name.Should().Be(originalName);
        }
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_RespetaCancellationToken()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new BusinessRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetAllWithDetailsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_NegocioExistenteConOnboardingCompleto_RetornaNegocioConDetalles()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var sector = new SectorTemplate
        {
            Id = 5,
            Name = "Spa & Bienestar"
        };

        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Serenidad Spa",
            Slug = "serenidad-spa",
            OnboardingCompletedAt = DateTime.UtcNow,
            SectorTemplate = sector,
            Brand = new BusinessBrand
            {
                BusinessId = businessId,
                LogoUrl = "https://cdn.slotify.app/logos/spa.png",
                PrimaryColor = "#0D9488",
                IsDefault = false
            },
            Services = new List<Service>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = "Masaje Relajante",
                    Price = 500m,
                    DurationMinutes = 60,
                    BufferTimeMinutes = 15,
                    IsActive = true,
                    DeletedAt = null
                }
            }
        };

        context.SectorTemplates.Add(sector);
        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetBySlugWithDetailsAsync("serenidad-spa");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(businessId);
        result.Name.Should().Be("Serenidad Spa");
        result.Slug.Should().Be("serenidad-spa");
        result.SectorTemplate.Should().NotBeNull();
        result.SectorTemplate!.Name.Should().Be("Spa & Bienestar");
        result.Brand.Should().NotBeNull();
        result.Brand!.LogoUrl.Should().Be("https://cdn.slotify.app/logos/spa.png");
        result.Brand!.PrimaryColor.Should().Be("#0D9488");
        result.Services.Should().ContainSingle();
        result.Services.First().Name.Should().Be("Masaje Relajante");
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_NegocioSinOnboardingCompleto_RetornaNull()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Negocio Incompleto",
            Slug = "negocio-incompleto",
            OnboardingCompletedAt = null // Onboarding pendiente
        };

        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetBySlugWithDetailsAsync("negocio-incompleto");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_SlugInexistente_RetornaNull()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetBySlugWithDetailsAsync("slug-que-no-existe");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_FiltraServiciosInactivosYEliminados()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var businessId = Guid.NewGuid();

        var business = new Business
        {
            Id = businessId,
            Name = "Centro Médico",
            Slug = "centro-medico",
            OnboardingCompletedAt = DateTime.UtcNow,
            Services = new List<Service>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = "Consulta Activa",
                    Price = 300m,
                    DurationMinutes = 30,
                    IsActive = true,
                    DeletedAt = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = "Consulta Inactiva",
                    Price = 300m,
                    DurationMinutes = 30,
                    IsActive = false,
                    DeletedAt = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = "Consulta Eliminada",
                    Price = 300m,
                    DurationMinutes = 30,
                    IsActive = true,
                    DeletedAt = DateTime.UtcNow
                }
            }
        };

        context.Businesses.Add(business);
        await context.SaveChangesAsync();

        var repository = new BusinessRepository(context);

        // Act
        var result = await repository.GetBySlugWithDetailsAsync("centro-medico");

        // Assert
        result.Should().NotBeNull();
        result!.Services.Should().HaveCount(1);
        result.Services.First().Name.Should().Be("Consulta Activa");
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_UsaAsNoTracking_NoRastreaEntidadesEnChangeTracker()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var businessId = Guid.NewGuid();

        await using (var seedContext = new AppDbContext(options))
        {
            seedContext.Businesses.Add(new Business
            {
                Id = businessId,
                Name = "Estudio Fotográfico",
                Slug = "estudio-foto",
                OnboardingCompletedAt = DateTime.UtcNow
            });
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert
        await using (var queryContext = new AppDbContext(options))
        {
            var repository = new BusinessRepository(queryContext);
            var result = await repository.GetBySlugWithDetailsAsync("estudio-foto");

            result.Should().NotBeNull();
            queryContext.ChangeTracker.Entries<Business>().Should().BeEmpty();
        }
    }

    [Fact]
    public async Task GetBySlugWithDetailsAsync_RespetaCancellationToken()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var repository = new BusinessRepository(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await repository.GetBySlugWithDetailsAsync("cualquier-slug", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
