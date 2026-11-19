using System.Diagnostics;
using MediatR;
using Slotify.Application.Common.Interfaces;

namespace Slotify.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior de observabilidad que instrumenta cada Command/Query de MediatR con:
/// 1. Trazas distribuidas (Activity / Span con TraceId y SpanId W3C).
/// 2. Métricas de latencia de casos de uso (ElapsedMs y alerta por umbral > 500ms).
/// 3. Registros estructurados (Logs en niveles DEBUG, INFO, WARNING y ERROR).
/// </summary>
public class ObservabilityBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    IObservabilityService? observabilityService = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly ActivitySource SlotifyActivitySource = new("Slotify.Application", "1.0.0");
    public const long SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var businessId = currentUserService.BusinessId;
        var userId = currentUserService.UserId;

        using var activity = SlotifyActivitySource.StartActivity($"MediatR.{requestName}", ActivityKind.Internal);
        var traceId = activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var spanId = activity?.SpanId.ToString() ?? Activity.Current?.SpanId.ToString() ?? Guid.NewGuid().ToString("N")[..16];

        activity?.SetTag("slotify.request.name", requestName);
        activity?.SetTag("slotify.tenant.business_id", businessId?.ToString() ?? "anonymous");
        activity?.SetTag("slotify.user.id", userId?.ToString() ?? "anonymous");

        observabilityService?.RecordSpanStart(traceId, spanId, requestName, businessId, userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var isSlow = elapsedMs > SlowRequestThresholdMs;

            activity?.SetTag("slotify.metrics.elapsed_ms", elapsedMs);
            activity?.SetStatus(ActivityStatusCode.Ok);

            observabilityService?.RecordMetricAndSpanEnd(traceId, spanId, requestName, elapsedMs, isSlow, businessId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            observabilityService?.RecordError(traceId, spanId, requestName, stopwatch.ElapsedMilliseconds, ex, businessId);

            throw;
        }
    }
}
