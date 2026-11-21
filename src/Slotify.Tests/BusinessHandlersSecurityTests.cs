namespace Slotify.Tests;

using FluentAssertions;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Businesses.Commands;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;
using System.Reflection;
using Xunit;

public class BusinessHandlersSecurityTests
{
    // =========================================================================
    // 1. Verificación de firma: ICurrentUserService obligatorio en constructores
    // =========================================================================
    [Theory]
    [InlineData(typeof(ChangeBusinessTemplateCommandHandler))]
    [InlineData(typeof(UpdateBusinessFormConfigCommandHandler))]
    [InlineData(typeof(UpdateBusinessBrandIdentityCommandHandler))]
    public void Handler_Constructor_RequiresCurrentUserServiceMandatory(Type handlerType)
    {
        var constructors = handlerType.GetConstructors();
        constructors.Should().NotBeEmpty();

        var primaryConstructor = constructors[0];
        var currentUserServiceParam = primaryConstructor
            .GetParameters()
            .FirstOrDefault(p => p.ParameterType == typeof(ICurrentUserService));

        currentUserServiceParam.Should().NotBeNull(
            $"El handler {handlerType.Name} debe tener un parámetro del tipo ICurrentUserService");

        currentUserServiceParam!.IsOptional.Should().BeFalse(
            $"El parámetro ICurrentUserService en {handlerType.Name} no debe ser opcional ni tener valor por defecto");

        currentUserServiceParam.HasDefaultValue.Should().BeFalse(
            $"El parámetro ICurrentUserService en {handlerType.Name} no debe tener valor por defecto null");
    }

    // =========================================================================
    // 2. ChangeBusinessTemplateCommandHandler
    // =========================================================================
    [Fact]
    public async Task ChangeBusinessTemplate_AuthenticatedUserModifyingOwnBusiness_Succeeds()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var templateId = 2;
        var business = new Business { Id = businessId, Name = "Mi Negocio", SectorTemplateId = 1 };
        var template = new SectorTemplate { Id = templateId, Name = "Plantilla 2" };

        var bizRepo = new Mock<IBusinessRepository>();
        bizRepo.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>())).ReturnsAsync(business);

        var tplRepo = new Mock<ISectorTemplateRepository>();
        tplRepo.Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>())).ReturnsAsync(template);

        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(businessId);

        var handler = new ChangeBusinessTemplateCommandHandler(bizRepo.Object, tplRepo.Object, uow.Object, currentUserService.Object);
        var command = new ChangeBusinessTemplateCommand(businessId, templateId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        business.SectorTemplateId.Should().Be(templateId);
        bizRepo.Verify(r => r.UpdateAsync(business, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeBusinessTemplate_AuthenticatedUserModifyingOtherBusiness_ReturnsForbidden()
    {
        // Arrange
        var ownBusinessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();

        var bizRepo = new Mock<IBusinessRepository>();
        var tplRepo = new Mock<ISectorTemplateRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(ownBusinessId);

        var handler = new ChangeBusinessTemplateCommandHandler(bizRepo.Object, tplRepo.Object, uow.Object, currentUserService.Object);
        var command = new ChangeBusinessTemplateCommand(otherBusinessId, 2);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        bizRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeBusinessTemplate_MissingBusinessIdInContext_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var tplRepo = new Mock<ISectorTemplateRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns((Guid?)null);

        var handler = new ChangeBusinessTemplateCommandHandler(bizRepo.Object, tplRepo.Object, uow.Object, currentUserService.Object);
        var command = new ChangeBusinessTemplateCommand(targetBusinessId, 2);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        bizRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeBusinessTemplate_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var tplRepo = new Mock<ISectorTemplateRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var handler = new ChangeBusinessTemplateCommandHandler(bizRepo.Object, tplRepo.Object, uow.Object, currentUserService.Object);
        var command = new ChangeBusinessTemplateCommand(targetBusinessId, 2);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    // =========================================================================
    // 3. UpdateBusinessFormConfigCommandHandler
    // =========================================================================
    [Fact]
    public async Task UpdateBusinessFormConfig_AuthenticatedUserModifyingOwnBusiness_Succeeds()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Mi Barbería" };

        var bizRepo = new Mock<IBusinessRepository>();
        bizRepo.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>())).ReturnsAsync(business);

        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(businessId);

        var handler = new UpdateBusinessFormConfigCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var jsonConfig = """
        {
            "accentColor": "#10B981",
            "secondaryColor": "#059669",
            "logoUrl": "https://cdn.slotify.app/logo.png",
            "businessName": "Mi Barbería Actualizada"
        }
        """;
        var command = new UpdateBusinessFormConfigCommand(businessId, jsonConfig);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        bizRepo.Verify(r => r.UpdateAsync(business, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBusinessFormConfig_AuthenticatedUserModifyingOtherBusiness_ReturnsForbidden()
    {
        // Arrange
        var ownBusinessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();

        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(ownBusinessId);

        var handler = new UpdateBusinessFormConfigCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessFormConfigCommand(otherBusinessId, "{}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        bizRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBusinessFormConfig_MissingBusinessIdInContext_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns((Guid?)null);

        var handler = new UpdateBusinessFormConfigCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessFormConfigCommand(targetBusinessId, "{}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateBusinessFormConfig_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var handler = new UpdateBusinessFormConfigCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessFormConfigCommand(targetBusinessId, "{}");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    // =========================================================================
    // 4. UpdateBusinessBrandIdentityCommandHandler
    // =========================================================================
    [Fact]
    public async Task UpdateBusinessBrandIdentity_AuthenticatedUserModifyingOwnBusiness_Succeeds()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var business = new Business { Id = businessId, Name = "Spa Aurora" };

        var bizRepo = new Mock<IBusinessRepository>();
        bizRepo.Setup(r => r.GetByIdAsync(businessId, It.IsAny<CancellationToken>())).ReturnsAsync(business);

        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(businessId);

        var handler = new UpdateBusinessBrandIdentityCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessBrandIdentityCommand(
            businessId,
            "https://cdn.slotify.app/logo.png",
            "#4F46E5",
            "#4338CA",
            "Spa Aurora Plus");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        bizRepo.Verify(r => r.UpdateAsync(business, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBusinessBrandIdentity_AuthenticatedUserModifyingOtherBusiness_ReturnsForbidden()
    {
        // Arrange
        var ownBusinessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();

        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns(ownBusinessId);

        var handler = new UpdateBusinessBrandIdentityCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessBrandIdentityCommand(
            otherBusinessId,
            "https://cdn.slotify.app/logo.png",
            "#4F46E5",
            "#4338CA");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        bizRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBusinessBrandIdentity_MissingBusinessIdInContext_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(true);
        currentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        currentUserService.Setup(s => s.BusinessId).Returns((Guid?)null);

        var handler = new UpdateBusinessBrandIdentityCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessBrandIdentityCommand(
            targetBusinessId,
            "https://cdn.slotify.app/logo.png",
            "#4F46E5",
            "#4338CA");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateBusinessBrandIdentity_UnauthenticatedUser_ReturnsForbidden()
    {
        // Arrange
        var targetBusinessId = Guid.NewGuid();
        var bizRepo = new Mock<IBusinessRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(s => s.IsAuthenticated).Returns(false);
        currentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var handler = new UpdateBusinessBrandIdentityCommandHandler(bizRepo.Object, uow.Object, currentUserService.Object);
        var command = new UpdateBusinessBrandIdentityCommand(
            targetBusinessId,
            "https://cdn.slotify.app/logo.png",
            "#4F46E5",
            "#4338CA");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }
}
