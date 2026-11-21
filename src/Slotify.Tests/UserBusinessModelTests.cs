namespace Slotify.Tests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Infrastructure.Data;

public class UserBusinessModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void AppDbContext_ContieneConfiguracionCorrecta_ParaUserBusiness()
    {
        // Arrange
        using var context = CreateContext();
        var model = context.Model;

        // Act
        var entityType = model.FindEntityType(typeof(UserBusiness));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("usuarios_negocios");

        // Primary Key
        var pk = entityType.FindPrimaryKey();
        pk.Should().NotBeNull();
        pk!.Properties.Select(p => p.GetColumnName()).Should().ContainSingle().Which.Should().Be("id");

        // Foreign Key User
        var fkUser = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        fkUser.Should().NotBeNull();
        fkUser!.Properties.Select(p => p.GetColumnName()).Should().ContainSingle().Which.Should().Be("id_usuario");
        fkUser.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        // Foreign Key Business
        var fkBusiness = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Business));
        fkBusiness.Should().NotBeNull();
        fkBusiness!.Properties.Select(p => p.GetColumnName()).Should().ContainSingle().Which.Should().Be("id_negocio");
        fkBusiness.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        // Composite Unique Index (id_usuario, id_negocio)
        var indexes = entityType.GetIndexes().ToList();
        var compositeIndex = indexes.FirstOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.GetColumnName() == "id_usuario") &&
            i.Properties.Any(p => p.GetColumnName() == "id_negocio"));

        compositeIndex.Should().NotBeNull("debe existir un índice único compuesto en (id_usuario, id_negocio)");

        // Navigations
        var userNav = entityType.FindNavigation(nameof(UserBusiness.User));
        userNav.Should().NotBeNull();
        userNav!.Inverse!.Name.Should().Be(nameof(User.UserBusinesses));

        var bizNav = entityType.FindNavigation(nameof(UserBusiness.Business));
        bizNav.Should().NotBeNull();
        bizNav!.Inverse!.Name.Should().Be(nameof(Business.UserBusinesses));
    }

    [Fact]
    public async Task AppDbContext_PuedePersistirYRecuperar_UserBusiness()
    {
        // Arrange
        using var context = CreateContext();

        var businessId = Guid.NewGuid();
        var business = new Business
        {
            Id = businessId,
            Name = "Negocio Multi-Inquilino",
            Slug = "negocio-multi",
            TimeZone = "America/Mexico_City"
        };

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "Carlos",
            LastName = "Propietario",
            Email = "carlos@propietario.com",
            Role = UserRole.Owner,
            BusinessId = businessId,
            Business = business
        };

        var membership = new UserBusiness
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessId = businessId,
            Role = UserRole.Owner,
            IsActive = true,
            User = user,
            Business = business
        };

        // Act
        context.Businesses.Add(business);
        context.Users.Add(user);
        context.UserBusinesses.Add(membership);
        await context.SaveChangesAsync();

        // Assert
        var persisted = await context.UserBusinesses
            .Include(ub => ub.User)
            .Include(ub => ub.Business)
            .FirstOrDefaultAsync(ub => ub.Id == membership.Id);

        persisted.Should().NotBeNull();
        persisted!.Role.Should().Be(UserRole.Owner);
        persisted.User.FullName.Should().Be("Carlos Propietario");
        persisted.Business.Name.Should().Be("Negocio Multi-Inquilino");
        persisted.IsActive.Should().BeTrue();

        // Verificar navegaciones inversas
        var userFromDb = await context.Users.Include(u => u.UserBusinesses).FirstAsync(u => u.Id == userId);
        userFromDb.UserBusinesses.Should().ContainSingle(ub => ub.BusinessId == businessId);

        var bizFromDb = await context.Businesses.Include(b => b.UserBusinesses).FirstAsync(b => b.Id == businessId);
        bizFromDb.UserBusinesses.Should().ContainSingle(ub => ub.UserId == userId);
    }
}
