using Slotify.API.Controllers;
using Slotify.Application.Common.Behaviors;
using Slotify.Application.Common.Interfaces;

namespace Slotify.API.Services;

/// <summary>
/// Implementación del servicio de observabilidad conectado a Serilog y al Dashboard en vivo para registrar
/// trazas distribuidas (TraceId / SpanId), métricas de rendimiento (API Response Time) y logs estructurados.
/// </summary>
public sealed class ObservabilityService(ILogger<ObservabilityService> logger) : IObservabilityService
{
    public void RecordSpanStart(string traceId, string spanId, string operationName, Guid? businessId, Guid? userId)
    {
        logger.LogDebug(
            "[TRACE:{TraceId}|SPAN:{SpanId}] Iniciando span MediatR.{OperationName} (BusinessId={BusinessId}, UserId={UserId})",
            traceId,
            spanId,
            operationName,
            businessId,
            userId);
    }

    public void RecordMetricAndSpanEnd(string traceId, string spanId, string operationName, long elapsedMs, bool isSlow, Guid? businessId)
    {
        TelemetryController.RecordInternalEvent(new MobileTelemetryEventDto
        {
            Category = isSlow ? "METRIC" : "TRACE",
            Level = isSlow ? "WARNING" : "INFO",
            Flow = $"MediatR.{operationName}",
            Message = isSlow
                ? $"[METRIC:SLOW_API] {operationName} excedió el umbral ({elapsedMs}ms > {ObservabilityBehavior<object, object>.SlowRequestThresholdMs}ms) [BusinessId={businessId}]"
                : $"[METRIC:API_RESPONSE_TIME] {operationName} completado en {elapsedMs}ms [BusinessId={businessId}]",
            TraceId = traceId,
            SpanId = spanId,
            DurationMs = elapsedMs,
            WasBufferedOffline = false,
            Timestamp = DateTime.UtcNow
        });

        if (isSlow)
        {
            logger.LogWarning(
                "[METRIC:SLOW_API][TRACE:{TraceId}|SPAN:{SpanId}] {OperationName} excedió el umbral de alerta ({ElapsedMs}ms > {ThresholdMs}ms) para BusinessId={BusinessId}",
                traceId,
                spanId,
                operationName,
                elapsedMs,
                ObservabilityBehavior<object, object>.SlowRequestThresholdMs,
                businessId);
        }
        else
        {
            logger.LogInformation(
                "[METRIC:API_RESPONSE_TIME][TRACE:{TraceId}|SPAN:{SpanId}] {OperationName} completado exitosamente en {ElapsedMs}ms (BusinessId={BusinessId})",
                traceId,
                spanId,
                operationName,
                elapsedMs,
                businessId);
        }
    }

    public void RecordError(string traceId, string spanId, string operationName, long elapsedMs, Exception exception, Guid? businessId)
    {
        TelemetryController.RecordInternalEvent(new MobileTelemetryEventDto
        {
            Category = "CRASH",
            Level = "ERROR",
            Flow = $"MediatR.{operationName}",
            Message = $"Excepción en {operationName} tras {elapsedMs}ms: {exception.Message}",
            TraceId = traceId,
            SpanId = spanId,
            DurationMs = elapsedMs,
            WasBufferedOffline = false,
            Timestamp = DateTime.UtcNow
        });

        logger.LogError(
            exception,
            "[TRACE:{TraceId}|SPAN:{SpanId}] Error crítico en {OperationName} tras {ElapsedMs}ms (BusinessId={BusinessId}): {ErrorMessage}",
            traceId,
            spanId,
            operationName,
            elapsedMs,
            businessId,
            exception.Message);
    }
}
