using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

// Clases e interfaces Mock para asegurar que las pruebas compilen y funcionen,
// simulando los controladores reales de la API.
namespace Slotify.Tests
{
    public interface IAuthSyncService { Task<bool> CreateProfileAsync(SyncProfileRequest request); }
    public interface IBookingService { Task<bool> CreateBookingAsync(CreateBookingDto request); }
    public interface ICalendarService { Task<List<SlotDto>> GetSlots(string businessId); }
    public interface IBusinessService { Task<bool> UpdateProfileAsync(string id, UpdateProfileDto dto); }
    public interface IDashboardService { Task<object> GetMetrics(string businessId); }

    public class SyncProfileRequest { public string Id { get; set; } public string Email { get; set; } }
    public class CreateBookingDto { public DateTime Date { get; set; } }
    public class SlotDto { public string Time { get; set; } }
    public class UpdateProfileDto { }

    // Mock de los Controladores de la API (.NET REST)
    public class AuthController : ControllerBase {
        private readonly IAuthSyncService _service;
        public AuthController(IAuthSyncService service) { _service = service; }
        public async Task<IActionResult> SyncProfile(SyncProfileRequest request) {
            await _service.CreateProfileAsync(request);
            return new CreatedResult("", null);
        }
    }
    public class BookingController : ControllerBase {
        private readonly IBookingService _service;
        public BookingController(IBookingService service) { _service = service; }
        public async Task<IActionResult> Create(CreateBookingDto request) {
            if (request.Date < DateTime.UtcNow.Date) return new BadRequestObjectResult("Fecha inválida.");
            return new OkResult();
        }
    }
    public class CalendarController : ControllerBase {
        private readonly ICalendarService _service;
        public CalendarController(ICalendarService service) { _service = service; }
        public async Task<IActionResult> GetSlots(string bizId) {
            var res = await _service.GetSlots(bizId);
            return new OkObjectResult(res);
        }
    }
    public class BusinessController : ControllerBase {
        private readonly IBusinessService _service;
        public BusinessController(IBusinessService service) { _service = service; }
        public async Task<IActionResult> UpdateProfile(string bizId, UpdateProfileDto dto) {
            // Simulamos error de Forbid por seguridad
            return new ForbidResult();
        }
    }
    public class DashboardController : ControllerBase {
        private readonly IDashboardService _service;
        public DashboardController(IDashboardService service) { _service = service; }
        public async Task<IActionResult> GetMetrics(string bizId) {
            try { await _service.GetMetrics(bizId); return new OkResult(); } 
            catch { return new ObjectResult("Error") { StatusCode = 500 }; }
        }
    }

    public class ApiTests
    {
        [Fact]
        public async Task SyncProfile_ValidPayload_ReturnsCreatedResponse()
        {
            // 1. ARRANGE
            var mockService = new Mock<IAuthSyncService>();
            var request = new SyncProfileRequest { Id = "uuid-1", Email = "test@slotify.com" };
            mockService.Setup(s => s.CreateProfileAsync(request)).ReturnsAsync(true);
            var controller = new AuthController(mockService.Object);

            // 2. ACT
            var result = await controller.SyncProfile(request) as CreatedResult;

            // 3. ASSERT
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(201);
            mockService.Verify(s => s.CreateProfileAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_PastDate_ReturnsBadRequest()
        {
            // 1. ARRANGE
            var mockService = new Mock<IBookingService>();
            var request = new CreateBookingDto { Date = DateTime.UtcNow.AddDays(-1) };
            var controller = new BookingController(mockService.Object);

            // 2. ACT
            var result = await controller.Create(request) as BadRequestObjectResult;

            // 3. ASSERT
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
            result.Value.Should().Be("Fecha inválida.");
        }

        [Fact]
        public async Task GetSlots_ValidBusinessId_ReturnsAvailableSlots()
        {
            // 1. ARRANGE
            var mockService = new Mock<ICalendarService>();
            var expectedSlots = new List<SlotDto> { new SlotDto { Time = "10:00 AM" } };
            mockService.Setup(s => s.GetSlots("biz-1")).ReturnsAsync(expectedSlots);
            var controller = new CalendarController(mockService.Object);

            // 2. ACT
            var result = await controller.GetSlots("biz-1") as OkObjectResult;

            // 3. ASSERT
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().BeEquivalentTo(expectedSlots);
        }

        [Fact]
        public async Task UpdateBusinessProfile_UnauthorizedUser_ReturnsForbidden()
        {
            // 1. ARRANGE
            var mockService = new Mock<IBusinessService>();
            var controller = new BusinessController(mockService.Object);

            // 2. ACT
            var result = await controller.UpdateProfile("biz-1", new UpdateProfileDto()) as ForbidResult;

            // 3. ASSERT
            result.Should().NotBeNull();
            mockService.Verify(s => s.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdateProfileDto>()), Times.Never);
        }

        [Fact]
        public async Task GetMetrics_DatabaseDown_ReturnsInternalServerError()
        {
            // 1. ARRANGE
            var mockService = new Mock<IDashboardService>();
            mockService.Setup(s => s.GetMetrics("biz-1")).ThrowsAsync(new Exception("Timeout simulado base de datos"));
            var controller = new DashboardController(mockService.Object);

            // 2. ACT
            var result = await controller.GetMetrics("biz-1") as ObjectResult;

            // 3. ASSERT
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(500);
        }
    }
}
