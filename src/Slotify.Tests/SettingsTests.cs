using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.Commands;
using Slotify.Application.Features.Businesses.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;

namespace Slotify.Tests;

public sealed class SettingsTests
{
    [Theory]
    [InlineData(UserRole.Owner, true)]
    [InlineData(UserRole.Employee, false)]
    public async Task EditarPerfil_SoloElPropietarioPuedeRenombrarElNegocio(UserRole role, bool allowed)
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Nombre anterior", Slug = "enlace-vigente" };
        var user = new User { Id = Guid.NewGuid(), BusinessId = business.Id, FirstName = "Anterior", Role = role };
        var session = new Mock<ICurrentUserService>();
        session.SetupGet(x => x.UserId).Returns(user.Id);
        session.SetupGet(x => x.BusinessId).Returns(business.Id);
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(user.Id, CancellationToken.None)).ReturnsAsync(user);
        var businesses = new Mock<IBusinessRepository>();
        businesses.Setup(x => x.GetByIdAsync(business.Id, CancellationToken.None)).ReturnsAsync(business);
        var writes = new Mock<IUnitOfWork>();
        var handler = new UpdateOwnProfileHandler(session.Object, users.Object, businesses.Object, writes.Object);

        var result = await handler.Handle(new("Ricardo", "Apellido", "Nombre comercial"), CancellationToken.None);

        Assert.Equal(allowed, result.Success);
        Assert.Equal(allowed ? "Nombre comercial" : "Nombre anterior", business.Name);
        Assert.Equal(allowed ? "Ricardo" : "Anterior", user.FirstName);
        Assert.Equal("enlace-vigente", business.Slug);
        writes.Verify(x => x.SaveChangesAsync(CancellationToken.None), allowed ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task InformacionDelNegocio_DevuelveElSlugYLosHorariosGeneralesDelNegocioAutenticado()
    {
        var business = new Business { Id = Guid.NewGuid(), Name = "Negocio de prueba", Slug = "slug-guardado" };
        var session = new Mock<ICurrentUserService>();
        var businesses = new Mock<IBusinessRepository>();
        var schedules = new Mock<IAvailabilityScheduleRepository>();
        var handler = new GetOwnBusinessInfoHandler(session.Object, businesses.Object,
            Mock.Of<ISectorTemplateRepository>(), schedules.Object);
        Assert.Equal(ErrorType.Forbidden, (await handler.Handle(new(), CancellationToken.None)).ErrorType);
        session.SetupGet(x => x.BusinessId).Returns(business.Id);
        businesses.Setup(x => x.GetByIdAsync(business.Id, CancellationToken.None)).ReturnsAsync(business);
        schedules.Setup(x => x.GetByBusinessIdAsync(business.Id, CancellationToken.None)).ReturnsAsync([
            new() { BusinessId = business.Id, DayOfWeek = 2, StartTime = new(9, 0), EndTime = new(13, 0) },
            new() { BusinessId = business.Id, DayOfWeek = 1, EmployeeId = Guid.NewGuid(), StartTime = new(7, 0), EndTime = new(10, 0) },
        ]);

        var result = await handler.Handle(new(), CancellationToken.None);

        Assert.Equal("slug-guardado", result.Data?.Slug);
        Assert.Equal(new TimeOnly(9, 0), Assert.Single(result.Data!.Hours).StartTime);
    }
}
