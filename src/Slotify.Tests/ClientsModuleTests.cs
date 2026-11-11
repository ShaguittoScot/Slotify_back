using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Clients.Commands;
using Slotify.Application.Features.Clients.Queries;
using Slotify.Domain.Entities;
using Slotify.Domain.Enums;
using Slotify.Domain.Interfaces;
using Xunit;

namespace Slotify.Tests;

/// <summary>
/// Pruebas unitarias y de integración en base de pruebas aislada para US-026 (Construcción de módulo de clientes).
/// Verifica:
/// 1. Seguridad e historial:
///    - Aislamiento estricto entre dos negocios (Negocio B no puede leer ni modificar fichas de Negocio A ni tabla global B2C `clientes`).
///    - PUT /api/clients/{id} con ID inexistente o de otro negocio retorna 404 NotFound sin buscar por correo ni crear cliente.
///    - No reasigna citas vinculadas a un ClientId distinto.
///    - No fusiona homónimos (mismo nombre, distinto teléfono/correo) ni familiares con teléfono compartido (distinto nombre).
/// 2. Cartera completa y métricas:
///    - Clientes registrados sin citas previas y sus notas internas persisten en `clientes_negocio` y aparecen al recargar GET /api/clients.
///    - Cero correos ficticios (@clientes.slotify.app / cliente@ejemplo.com).
///    - TotalSpent suma el importe pactado (AgreedTotal) de las citas completadas.
/// </summary>
public class ClientsModuleTests
{
    private const string TestJwtSecret = "TestSecretKey_ForUS026_AtLeast32CharsLong!!";

    /// <summary>
    /// Base de datos en memoria aislada para pruebas deterministas multi-negocio sin tocar Supabase.
    /// </summary>
    private sealed class IsolatedClientsTestDatabase
    {
        public List<BusinessClient> BusinessClients { get; } = [];
        public List<Appointment> Appointments { get; } = [];
        public List<Client> GlobalClients { get; } = [];
        public int SaveChangesCount { get; private set; }

        public Mock<IBusinessClientRepository> CreateBusinessClientRepoMock()
        {
            var mock = new Mock<IBusinessClientRepository>();

            mock.Setup(r => r.GetByBusinessIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid bizId, CancellationToken _) =>
                    BusinessClients
                        .Where(c => c.BusinessId == bizId)
                        .OrderByDescending(c => c.UpdatedAt)
                        .ToList());

            mock.Setup(r => r.GetByIdAndBusinessIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, Guid bizId, CancellationToken _) =>
                    BusinessClients.FirstOrDefault(c =>
                        c.BusinessId == bizId && (c.Id == id || c.ClientId == id)));

            mock.Setup(r => r.AddAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BusinessClient entity, CancellationToken _) =>
                {
                    BusinessClients.Add(entity);
                    return entity;
                });

            mock.Setup(r => r.UpdateAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()))
                .Returns((BusinessClient entity, CancellationToken _) =>
                {
                    var idx = BusinessClients.FindIndex(c => c.Id == entity.Id && c.BusinessId == entity.BusinessId);
                    if (idx >= 0)
                        BusinessClients[idx] = entity;
                    return Task.CompletedTask;
                });

            mock.Setup(r => r.DeleteAsync(It.IsAny<BusinessClient>(), It.IsAny<CancellationToken>()))
                .Returns((BusinessClient entity, CancellationToken _) =>
                {
                    BusinessClients.Remove(entity);
                    return Task.CompletedTask;
                });

            return mock;
        }

        public Mock<IAppointmentRepository> CreateAppointmentRepoMock()
        {
            var mock = new Mock<IAppointmentRepository>();

            mock.Setup(r => r.GetByBusinessIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid bizId, CancellationToken _) =>
                    Appointments
                        .Where(a => a.BusinessId == bizId)
                        .OrderByDescending(a => a.StartTime)
                        .ToList());

            mock.Setup(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
                .Returns((Appointment entity, CancellationToken _) =>
                {
                    var idx = Appointments.FindIndex(a => a.Id == entity.Id);
                    if (idx >= 0)
                        Appointments[idx] = entity;
                    return Task.CompletedTask;
                });

            return mock;
        }

        public Mock<IUnitOfWork> CreateUnitOfWorkMock()
        {
            var mock = new Mock<IUnitOfWork>();
            mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    SaveChangesCount++;
                    return 1;
                });
            return mock;
        }
    }

    private static ICurrentUserService CreateCurrentUserForBusiness(Guid? businessId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.BusinessId).Returns(businessId);
        return mock.Object;
    }

    [Fact]
    public async Task GetBusinessClients_SinBusinessIdEnContexto_RetornaForbidden()
    {
        var db = new IsolatedClientsTestDatabase();
        var bizClientRepo = db.CreateBusinessClientRepoMock();
        var apptRepo = db.CreateAppointmentRepoMock();

        var handler = new GetBusinessClientsHandler(
            bizClientRepo.Object,
            apptRepo.Object,
            CreateCurrentUserForBusiness(null));

        var result = await handler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        apptRepo.Verify(r => r.GetByBusinessIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBusinessClients_NegocioSinCitasNiClientes_RetornaListaVaciaSinDatosFantasmas()
    {
        var businessId = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();

        var handler = new GetBusinessClientsHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            CreateCurrentUserForBusiness(businessId));

        var result = await handler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBusinessClients_AgrupaCitasPorClienteYCalculaTotalSpentSoloDeCompletadasAislandoNegocio()
    {
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var db = new IsolatedClientsTestDatabase();
        db.Appointments.AddRange([
            new Appointment
            {
                Id = Guid.NewGuid(),
                BusinessId = businessA,
                ClientName = "Carlos Mendoza",
                ClientEmail = "carlos@correo.com",
                ClientPhone = "+52 442 111 2233",
                StartTime = now.AddDays(-10),
                EndTime = now.AddDays(-10).AddHours(1),
                Status = AppointmentStatus.Completed,
                AgreedTotal = 250m
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                BusinessId = businessA,
                ClientName = "Carlos Mendoza",
                ClientEmail = "carlos@correo.com",
                ClientPhone = "+52 442 111 2233",
                StartTime = now.AddDays(2),
                EndTime = now.AddDays(2).AddHours(1),
                Status = AppointmentStatus.Confirmed,
                AgreedTotal = 300m // Importe pactado (confirmada, aún no cobrada)
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                BusinessId = businessA,
                ClientName = "Carlos Mendoza",
                ClientEmail = "carlos@correo.com",
                ClientPhone = "+52 442 111 2233",
                StartTime = now.AddDays(-3),
                EndTime = now.AddDays(-3).AddHours(1),
                Status = AppointmentStatus.Cancelled,
                AgreedTotal = 200m
            },
            new Appointment
            {
                Id = Guid.NewGuid(),
                BusinessId = businessB, // Otro negocio: no debe filtrarse a Negocio A
                ClientName = "Cliente Intruso",
                ClientEmail = "intruso@otro.com",
                ClientPhone = "+52 555 999 8888",
                StartTime = now.AddDays(-1),
                EndTime = now.AddDays(-1).AddHours(1),
                Status = AppointmentStatus.Completed,
                AgreedTotal = 999m
            }
        ]);

        var handler = new GetBusinessClientsHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            CreateCurrentUserForBusiness(businessA));

        var result = await handler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);

        var carlos = result.Data![0];
        carlos.FullName.Should().Be("Carlos Mendoza");
        carlos.Email.Should().Be("carlos@correo.com");
        carlos.Phone.Should().Be("+52 442 111 2233");
        carlos.TotalVisits.Should().Be(2); // 1 completada + 1 confirmada
        carlos.CompletedVisits.Should().Be(1);
        carlos.CancelledVisits.Should().Be(1);
        carlos.TotalSpent.Should().Be(250m); // Solo suma la cita completada ($250), nunca la confirmada ($300)
        carlos.LastVisitAt.Should().NotBeNull();
        carlos.NextVisitAt.Should().NotBeNull();
        carlos.Appointments.Should().HaveCount(3);
    }

    [Fact]
    public async Task AislamientoDosNegocios_NegocioBNoPuedeModificarFichaDeNegocioANiFichaGlobal_PutIdInexistenteRetorna404SinCrearCliente()
    {
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();
        var globalB2CClientId = Guid.NewGuid();

        var db = new IsolatedClientsTestDatabase();

        // Cliente global B2C existente en la plataforma
        var globalClient = new Client
        {
            Id = globalB2CClientId,
            FirstName = "Elena",
            LastName = "Global",
            Email = "elena@global.com",
            Phone = "+52 442 000 1111"
        };
        db.GlobalClients.Add(globalClient);

        // Negocio A registra su propia ficha de cliente
        var upsertHandlerA = new UpsertBusinessClientHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            db.CreateUnitOfWorkMock().Object,
            CreateCurrentUserForBusiness(businessA));

        var createInAResult = await upsertHandlerA.Handle(new UpsertBusinessClientCommand
        {
            FirstName = "Elena",
            LastName = "Negocio A",
            Email = "elena@global.com",
            Phone = "+52 442 111 9999",
            Notes = "Nota privada de Negocio A"
        }, CancellationToken.None);

        createInAResult.Success.Should().BeTrue();
        var clientAId = Guid.Parse(createInAResult.Data!.Id);

        // La ficha global B2C permanece intacta
        globalClient.FirstName.Should().Be("Elena");
        globalClient.LastName.Should().Be("Global");
        globalClient.Phone.Should().Be("+52 442 000 1111");

        // Negocio B intenta hacer PUT usando el ID de la ficha de Negocio A y el mismo correo
        var upsertHandlerB = new UpsertBusinessClientHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            db.CreateUnitOfWorkMock().Object,
            CreateCurrentUserForBusiness(businessB));

        var crossTenantPutResult = await upsertHandlerB.Handle(new UpsertBusinessClientCommand
        {
            ClientId = clientAId,
            IsExplicitUpdateById = true,
            FirstName = "Elena Hackeada",
            LastName = "Por Negocio B",
            Email = "elena@global.com",
            Phone = "+52 442 888 7777",
            Notes = "Intento de sobrescritura"
        }, CancellationToken.None);

        // Debe ser rechazado con NotFound (404), sin tocar la ficha de Negocio A y sin crear un registro en Negocio B
        crossTenantPutResult.Success.Should().BeFalse();
        crossTenantPutResult.ErrorType.Should().Be(ErrorType.NotFound);

        db.BusinessClients.Where(c => c.BusinessId == businessB).Should().BeEmpty();
        var untouchedA = db.BusinessClients.Single(c => c.BusinessId == businessA);
        untouchedA.FullName.Should().Be("Elena Negocio A");
        untouchedA.Notes.Should().Be("Nota privada de Negocio A");

        // Negocio B intenta un PUT con un ID aleatorio inexistente
        var nonExistentPutResult = await upsertHandlerB.Handle(new UpsertBusinessClientCommand
        {
            ClientId = Guid.NewGuid(),
            IsExplicitUpdateById = true,
            FirstName = "Fantasma",
            LastName = "Inexistente",
            Email = "elena@global.com",
            Phone = "+52 442 777 6666"
        }, CancellationToken.None);

        nonExistentPutResult.Success.Should().BeFalse();
        nonExistentPutResult.ErrorType.Should().Be(ErrorType.NotFound);
        db.BusinessClients.Where(c => c.BusinessId == businessB).Should().BeEmpty();
    }

    [Fact]
    public async Task ProteccionHistorial_NoReasignaCitasConClientIdDistinto_NoFusionaHomonimosNiTelefonoCompartido()
    {
        var businessId = Guid.NewGuid();
        var existingClientId = Guid.NewGuid();
        var otherClientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var db = new IsolatedClientsTestDatabase();

        // 1. Dos homónimos ("Juan Pérez") con distinto teléfono y una pareja con teléfono compartido ("Ana Pérez" y "Juan Pérez")
        var apptJuan1 = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ClientId = existingClientId, // Ya vinculada a Juan #1
            ClientName = "Juan Pérez",
            ClientEmail = "juan1@correo.com",
            ClientPhone = "4421110001",
            StartTime = now.AddDays(-5),
            EndTime = now.AddDays(-5).AddHours(1),
            Status = AppointmentStatus.Completed,
            AgreedTotal = 200m
        };

        var apptJuan2Unlinked = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ClientId = null, // Homónimo sin ClientId pero con otro teléfono
            ClientName = "Juan Pérez",
            ClientEmail = "juan2@correo.com",
            ClientPhone = "4422220002",
            StartTime = now.AddDays(-2),
            EndTime = now.AddDays(-2).AddHours(1),
            Status = AppointmentStatus.Completed,
            AgreedTotal = 350m
        };

        var apptAnaSharedPhone = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ClientId = otherClientId, // Distinto nombre, mismo teléfono que Juan #1
            ClientName = "Ana Pérez",
            ClientEmail = "",
            ClientPhone = "4421110001",
            StartTime = now.AddDays(-1),
            EndTime = now.AddDays(-1).AddHours(1),
            Status = AppointmentStatus.Completed,
            AgreedTotal = 500m
        };

        db.Appointments.AddRange([apptJuan1, apptJuan2Unlinked, apptAnaSharedPhone]);
        var appointmentRepository = db.CreateAppointmentRepoMock();

        // Registrar/actualizar a Juan #2 (homónimo con teléfono 4422220002)
        var upsertHandler = new UpsertBusinessClientHandler(
            db.CreateBusinessClientRepoMock().Object,
            appointmentRepository.Object,
            db.CreateUnitOfWorkMock().Object,
            CreateCurrentUserForBusiness(businessId));

        var upsertJuan2 = await upsertHandler.Handle(new UpsertBusinessClientCommand
        {
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan2@correo.com",
            Phone = "4422220002"
        }, CancellationToken.None);

        upsertJuan2.Success.Should().BeTrue();
        upsertJuan2.Data!.TotalVisits.Should().Be(1);
        upsertJuan2.Data.TotalSpent.Should().Be(350m);

        apptJuan1.ClientId.Should().Be(existingClientId);
        apptJuan2Unlinked.ClientId.Should().BeNull();
        apptAnaSharedPhone.ClientId.Should().Be(otherClientId);
        appointmentRepository.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);

        // Consultar la cartera completa del negocio: deben existir 3 clientes separados (Juan #1, Juan #2 y Ana Pérez)
        var queryHandler = new GetBusinessClientsHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            CreateCurrentUserForBusiness(businessId));

        var allClientsResult = await queryHandler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);
        allClientsResult.Success.Should().BeTrue();
        allClientsResult.Data.Should().HaveCount(3);
        var savedJuan2 = allClientsResult.Data!.Single(client => client.Id == upsertJuan2.Data.Id);
        savedJuan2.Appointments.Should().ContainSingle().Which.Id.Should().Be(apptJuan2Unlinked.Id);

        var edit = await upsertHandler.Handle(new UpsertBusinessClientCommand
        {
            ClientId = savedJuan2.ClientId,
            IsExplicitUpdateById = true,
            FirstName = "Juan",
            LastName = "Pérez",
            Email = "juan-nuevo@example.invalid",
            Phone = "4421234567"
        }, CancellationToken.None);
        edit.Success.Should().BeTrue();
        var afterEdit = await queryHandler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);
        afterEdit.Data.Should().HaveCount(3);
        afterEdit.Data!.Single(client => client.Id == savedJuan2.Id)
            .Appointments.Should().ContainSingle().Which.Id.Should().Be(apptJuan2Unlinked.Id);
    }

    [Fact]
    public async Task HistorialGlobal_UsaFichasDistintasPorNegocioYSuIdParaEditar()
    {
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();
        var globalClientId = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();
        var now = DateTime.UtcNow;
        foreach (var businessId in new[] { businessA, businessB })
        {
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                ClientId = globalClientId,
                ClientName = "María López",
                StartTime = now.AddDays(-1),
                EndTime = now.AddDays(-1).AddHours(1),
                Status = AppointmentStatus.Completed
            });
        }
        var clientRepository = db.CreateBusinessClientRepoMock();
        var appointmentRepository = db.CreateAppointmentRepoMock();
        var unitOfWork = db.CreateUnitOfWorkMock();
        var currentUserA = CreateCurrentUserForBusiness(businessA);
        var handlerA = new UpsertBusinessClientHandler(clientRepository.Object, appointmentRepository.Object, unitOfWork.Object, currentUserA);
        var handlerB = new UpsertBusinessClientHandler(clientRepository.Object, appointmentRepository.Object, unitOfWork.Object, CreateCurrentUserForBusiness(businessB));
        var command = new UpsertBusinessClientCommand
        {
            ClientId = globalClientId,
            IsExplicitUpdateById = true,
            FirstName = "María",
            LastName = "López",
            Email = "maria@correo.com"
        };

        var savedA = await handlerA.Handle(command, CancellationToken.None);
        var savedB = await handlerB.Handle(command, CancellationToken.None);

        savedA.Success.Should().BeTrue();
        savedB.Success.Should().BeTrue();
        var cardA = db.BusinessClients.Single(c => c.BusinessId == businessA);
        var cardB = db.BusinessClients.Single(c => c.BusinessId == businessB);
        cardA.Id.Should().NotBe(cardB.Id).And.NotBe(globalClientId);
        cardB.Id.Should().NotBe(globalClientId);
        cardA.ClientId.Should().Be(globalClientId);
        cardB.ClientId.Should().Be(globalClientId);
        savedA.Data!.ClientId.Should().Be(cardA.Id);
        savedB.Data!.ClientId.Should().Be(cardB.Id);

        var updatedA = await handlerA.Handle(command with
        {
            ClientId = cardA.Id,
            Notes = "Prefiere horario matutino"
        }, CancellationToken.None);

        updatedA.Success.Should().BeTrue();
        updatedA.Data!.ClientId.Should().Be(cardA.Id);
        db.BusinessClients.Should().HaveCount(2);
        cardB.Notes.Should().BeNull();

        var queryHandler = new GetBusinessClientsHandler(clientRepository.Object, appointmentRepository.Object, currentUserA);
        var list = await queryHandler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);
        var listedCard = list.Data.Should().ContainSingle().Which;
        listedCard.ClientId.Should().Be(savedA.Data.ClientId);
        listedCard.Notes.Should().Be("Prefiere horario matutino");
        listedCard.Appointments.Should().ContainSingle().Which.Id.Should().Be(db.Appointments[0].Id);
        db.Appointments.Should().OnlyContain(a => a.ClientId == globalClientId && a.ClientName == "María López");
        appointmentRepository.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ArchivarYRestaurar_HistorialSinContacto_NoReapareceNiModificaLaCita()
    {
        var businessId = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();
        var now = DateTime.UtcNow;
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ClientName = "Cliente sin contacto",
            StartTime = now.AddHours(-2),
            EndTime = now.AddHours(-1),
            Status = AppointmentStatus.Completed
        };
        db.Appointments.Add(appointment);
        var clients = db.CreateBusinessClientRepoMock();
        var appointments = db.CreateAppointmentRepoMock();
        var unitOfWork = db.CreateUnitOfWorkMock();
        var user = CreateCurrentUserForBusiness(businessId);
        var manage = new ManageBusinessClientHandler(clients.Object, appointments.Object, unitOfWork.Object, user);
        var load = new GetBusinessClientsHandler(clients.Object, appointments.Object, user);

        var archived = await manage.Handle(new(BusinessClientAction.Archive, null, appointment.Id), CancellationToken.None);
        archived.Success.Should().BeTrue();
        archived.Data!.ArchivedAt.Should().NotBeNull();
        var cardId = archived.Data.ClientId;
        var repeated = await manage.Handle(new(BusinessClientAction.Archive, null, appointment.Id), CancellationToken.None);
        repeated.Data!.ClientId.Should().Be(cardId);
        db.BusinessClients.Should().ContainSingle();
        (await load.Handle(new(), CancellationToken.None)).Data.Should().BeEmpty();
        var all = await load.Handle(new() { IncludeArchived = true }, CancellationToken.None);
        all.Data.Should().ContainSingle().Which.Appointments.Should().ContainSingle().Which.Id.Should().Be(appointment.Id);

        var otherBusiness = CreateCurrentUserForBusiness(Guid.NewGuid());
        var foreign = new ManageBusinessClientHandler(clients.Object, appointments.Object, unitOfWork.Object, otherBusiness);
        (await foreign.Handle(new(BusinessClientAction.Restore, cardId), CancellationToken.None)).ErrorType.Should().Be(ErrorType.NotFound);
        (await foreign.Handle(new(BusinessClientAction.Archive, null, appointment.Id), CancellationToken.None)).ErrorType.Should().Be(ErrorType.NotFound);

        var restored = await manage.Handle(new(BusinessClientAction.Restore, cardId), CancellationToken.None);
        restored.Success.Should().BeTrue();
        restored.Data!.ArchivedAt.Should().BeNull();
        var active = await load.Handle(new(), CancellationToken.None);
        active.Data.Should().ContainSingle().Which.Appointments.Should().ContainSingle().Which.Id.Should().Be(appointment.Id);
        appointment.ClientId.Should().BeNull();
        appointments.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Eliminar_SoloPermiteFichaPropiaSinCitasInclusoSiLasCitasEstanCanceladas()
    {
        var businessId = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();
        var own = new BusinessClient { Id = Guid.NewGuid(), BusinessId = businessId, FirstName = "Sin historial" };
        var withHistory = new BusinessClient
        {
            Id = Guid.NewGuid(), BusinessId = businessId, FirstName = "Con historial", Email = "historial@example.invalid"
        };
        var foreign = new BusinessClient { Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), FirstName = "Otro negocio" };
        db.BusinessClients.AddRange([own, withHistory, foreign]);
        db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ClientName = withHistory.FirstName,
            ClientEmail = withHistory.Email,
            Status = AppointmentStatus.Cancelled
        });
        var handler = new ManageBusinessClientHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            db.CreateUnitOfWorkMock().Object,
            CreateCurrentUserForBusiness(businessId));

        (await handler.Handle(new(BusinessClientAction.Delete, foreign.Id), CancellationToken.None)).ErrorType.Should().Be(ErrorType.NotFound);
        (await handler.Handle(new(BusinessClientAction.Delete, withHistory.Id), CancellationToken.None)).ErrorType.Should().Be(ErrorType.Conflict);
        (await handler.Handle(new(BusinessClientAction.Delete, own.Id), CancellationToken.None)).Success.Should().BeTrue();

        db.BusinessClients.Should().BeEquivalentTo([withHistory, foreign]);
        db.Appointments.Should().ContainSingle();
        db.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task CarteraCompleta_ClienteSinCitasYConNotasPersisteTrasRecargaSinCorreoFicticio()
    {
        var businessId = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();

        var upsertHandler = new UpsertBusinessClientHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            db.CreateUnitOfWorkMock().Object,
            CreateCurrentUserForBusiness(businessId));

        // Registrar cliente únicamente con teléfono y notas, sin correo ni citas
        var createResult = await upsertHandler.Handle(new UpsertBusinessClientCommand
        {
            FirstName = "Lucía",
            LastName = "Ramírez",
            Email = "",
            Phone = "+52 442 900 8000",
            Notes = "Prefiere atención matutina y café americano"
        }, CancellationToken.None);

        createResult.Success.Should().BeTrue();
        createResult.Data!.Email.Should().BeEmpty(); // Sin correo sintético @clientes.slotify.app
        createResult.Data.Notes.Should().Be("Prefiere atención matutina y café americano");
        createResult.Data.TotalVisits.Should().Be(0);

        // Simular recarga o acceso desde otro dispositivo vía GET /api/clients
        var queryHandler = new GetBusinessClientsHandler(
            db.CreateBusinessClientRepoMock().Object,
            db.CreateAppointmentRepoMock().Object,
            CreateCurrentUserForBusiness(businessId));

        var reloadResult = await queryHandler.Handle(new GetBusinessClientsQuery(), CancellationToken.None);

        reloadResult.Success.Should().BeTrue();
        reloadResult.Data.Should().HaveCount(1);
        var lucia = reloadResult.Data![0];
        lucia.FullName.Should().Be("Lucía Ramírez");
        lucia.Phone.Should().Be("+52 442 900 8000");
        lucia.Email.Should().BeEmpty();
        lucia.Notes.Should().Be("Prefiere atención matutina y café americano");
        lucia.TotalVisits.Should().Be(0);
    }

    [Fact]
    public void UpsertBusinessClientValidator_ValidaNombreMediosDeContactoYLongitudDeNotas()
    {
        var validator = new UpsertBusinessClientValidator();

        var invalidNoContact = new UpsertBusinessClientCommand
        {
            FirstName = "Laura",
            LastName = "Pérez",
            Email = "",
            Phone = ""
        };
        validator.Validate(invalidNoContact).IsValid.Should().BeFalse();

        var validWithPhoneAndNotes = new UpsertBusinessClientCommand
        {
            FirstName = "Laura",
            LastName = "Pérez",
            Email = "",
            Phone = "+52 442 333 4455",
            Notes = "Nota válida"
        };
        validator.Validate(validWithPhoneAndNotes).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task HttpPipeline_DosNegocios_AislamientoYGestionDeFichasIncluidasNotasSinContacto()
    {
        var businessA = Guid.NewGuid();
        var businessB = Guid.NewGuid();
        var db = new IsolatedClientsTestDatabase();
        var bizClientRepoMock = db.CreateBusinessClientRepoMock();
        var apptRepoMock = db.CreateAppointmentRepoMock();
        var uowMock = db.CreateUnitOfWorkMock();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = TestJwtSecret,
                        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=slotify_test;Username=postgres;Password=postgres"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<Slotify.Infrastructure.Data.AppDbContext>>();
                    services.AddDbContext<Slotify.Infrastructure.Data.AppDbContext>(options =>
                        Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsBuilderExtensions.UseNpgsql(
                            options,
                            "Host=127.0.0.1;Port=1;Database=slotify_isolated_test;Username=postgres;Password=postgres;Timeout=1"));

                    services.RemoveAll<IBusinessClientRepository>();
                    services.RemoveAll<IAppointmentRepository>();
                    services.RemoveAll<IUnitOfWork>();
                    services.AddScoped(_ => bizClientRepoMock.Object);
                    services.AddScoped(_ => apptRepoMock.Object);
                    services.AddScoped(_ => uowMock.Object);
                });
            });

        using var client = factory.CreateClient();

        // 1. Sin token -> 401 Unauthorized
        var unauthResponse = await client.GetAsync("/api/clients");
        unauthResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Autenticado como Negocio A -> POST /api/clients crea cliente sin citas
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(businessA));

        var postResponseA = await client.PostAsJsonAsync("/api/clients", new
        {
            firstName = "Valeria",
            lastName = "Soto",
            email = "",
            phone = "+52 442 123 4567",
            notes = "Cliente VIP de Negocio A"
        });
        postResponseA.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. GET /api/clients en Negocio A devuelve el cliente recién creado
        var getResponseA = await client.GetAsync("/api/clients");
        getResponseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyA = await getResponseA.Content.ReadAsStringAsync();
        bodyA.Should().Contain("Valeria Soto");
        bodyA.Should().Contain("Cliente VIP de Negocio A");

        var createdId = db.BusinessClients.Single(c => c.BusinessId == businessA).Id;

        // 4. Autenticado como Negocio B -> GET /api/clients está vacío y PUT /api/clients/{createdId} devuelve 404 NotFound
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(businessB));

        var getResponseB = await client.GetAsync("/api/clients");
        getResponseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyB = await getResponseB.Content.ReadAsStringAsync();
        bodyB.Should().NotContain("Valeria Soto");

        var putResponseB = await client.PutAsJsonAsync($"/api/clients/{createdId}", new
        {
            firstName = "Valeria Modificada",
            lastName = "Por Negocio B",
            email = "",
            phone = "+52 442 999 0000"
        });
        putResponseB.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/clients/{createdId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(businessA));
        (await client.PostAsJsonAsync("/api/clients/archive", new { clientId = createdId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetStringAsync("/api/clients")).Should().NotContain("Valeria Soto");
        (await client.GetStringAsync("/api/clients?includeArchived=true")).Should().Contain("Valeria Soto");
        (await client.PostAsync($"/api/clients/{createdId}/restore", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetStringAsync("/api/clients")).Should().Contain("Valeria Soto");
        (await client.DeleteAsync($"/api/clients/{createdId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        db.BusinessClients.Should().BeEmpty();

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BusinessId = businessA,
            ClientName = "Cliente histórico",
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow.AddHours(-1),
            Status = AppointmentStatus.Completed
        };
        db.Appointments.Add(appointment);
        const string note = "Prefiere horario matutino";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(businessB));
        (await client.PatchAsJsonAsync("/api/clients/note", new { appointmentId = appointment.Id, note }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        db.BusinessClients.Should().BeEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt(businessA));
        (await client.PatchAsJsonAsync("/api/clients/note", new { appointmentId = appointment.Id, note }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var historicalCard = db.BusinessClients.Should().ContainSingle().Which;
        historicalCard.Phone.Should().BeNullOrEmpty();
        historicalCard.Email.Should().BeEmpty();
        historicalCard.HistoryAppointmentIds.Should().Equal(appointment.Id);
        (await client.GetStringAsync("/api/clients")).Should().Contain(note).And.Contain(appointment.Id.ToString("D"));
        (await client.PatchAsJsonAsync("/api/clients/note", new { clientId = historicalCard.Id, note = "" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetStringAsync("/api/clients")).Should().NotContain(note);
        apptRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string GenerateJwt(Guid businessId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestJwtSecret);
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Slotify.Infrastructure.Services.JwtService.Issuer,
            Audience = Slotify.Infrastructure.Services.JwtService.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("businessId", businessId.ToString())
            ]),
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(20),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });
        return tokenHandler.WriteToken(token);
    }
}
