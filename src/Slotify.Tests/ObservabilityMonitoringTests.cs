using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Slotify.API.Controllers;
using Slotify.Application.Common.Behaviors;
using Slotify.Application.Common.Interfaces;
using Xunit;

namespace Slotify.Tests;

public class ObservabilityMonitoringTests
{
    public sealed record SampleCalendarQuery(Guid BusinessId);

    [Fact]
    public async Task ObservabilityBehavior_RecordsTraceSpanAndApiResponseTimeMetric()
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUserService>();
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        currentUserMock.Setup(u => u.BusinessId).Returns(businessId);
        currentUserMock.Setup(u => u.UserId).Returns(userId);

        var observabilityMock = new Mock<IObservabilityService>();
        var behavior = new ObservabilityBehavior<SampleCalendarQuery, string>(
            currentUserMock.Object,
            observabilityMock.Object);

        // Act
        var result = await behavior.Handle(
            new SampleCalendarQuery(businessId),
            () => Task.FromResult("SlotsLoaded"),
            CancellationToken.None);

        // Assert
        result.Should().Be("SlotsLoaded");
        observabilityMock.Verify(
            o => o.RecordSpanStart(
                It.Is<string>(t => !string.IsNullOrWhiteSpace(t)),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                nameof(SampleCalendarQuery),
                businessId,
                userId),
            Times.Once);

        observabilityMock.Verify(
            o => o.RecordMetricAndSpanEnd(
                It.IsAny<string>(),
                It.IsAny<string>(),
                nameof(SampleCalendarQuery),
                It.Is<long>(ms => ms >= 0),
                false,
                businessId),
            Times.Once);
    }

    [Fact]
    public async Task TelemetryController_IngestBatch_ProcessesBufferedLogsMetricsAndTraces()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TelemetryController>>();
        var controller = new TelemetryController(loggerMock.Object);

        var batchRequest = new MobileTelemetryBatchRequest
        {
            DeviceId = "pixel-8-pro-slotify",
            Platform = "Android 15 (React Native 0.88 / Expo 58)",
            AppVersion = "1.0.0",
            Events =
            [
                new MobileTelemetryEventDto
                {
                    Category = "TRACE",
                    Level = "INFO",
                    Flow = "BookingCheckout_CreateAppointment",
                    Message = "Span 3: POST /api/calendar/appointments completado",
                    TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
                    SpanId = "00f067aa0ba902b7",
                    DurationMs = 248.4,
                    WasBufferedOffline = false
                },
                new MobileTelemetryEventDto
                {
                    Category = "LOG",
                    Level = "WARNING",
                    Flow = "ExploreCatalog_LoadBusinesses",
                    Message = "Timeout en GET /api/businesses; activando DEFAULT_BUSINESS_CATALOG offline fallback",
                    TraceId = "4bf92f3577b34da6a3ce929d0e0e4736",
                    SpanId = "a2fb4a1d1a96d312",
                    DurationMs = 1500.0,
                    WasBufferedOffline = true
                }
            ]
        };

        // Act
        var actionResult = controller.IngestBatch(batchRequest) as OkObjectResult;

        // Assert
        actionResult.Should().NotBeNull();
        actionResult!.StatusCode.Should().Be(200);
    }
}

