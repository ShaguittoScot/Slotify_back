namespace Slotify.Application.Common.Interfaces;

/// <summary>
/// Abstracción del servicio de observabilidad (Métricas, Registros y Trazas) para la capa Application.
/// </summary>
public interface IObservabilityService
{
    void RecordSpanStart(string traceId, string spanId, string operationName, Guid? businessId, Guid? userId);
    void RecordMetricAndSpanEnd(string traceId, string spanId, string operationName, long elapsedMs, bool isSlow, Guid? businessId);
    void RecordError(string traceId, string spanId, string operationName, long elapsedMs, Exception exception, Guid? businessId);
}

