using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Slotify.API.Controllers;

/// <summary>
/// Endpoint receptor y consola visual de telemetría móvil (Log Buffering, Métricas de cliente y Spans de Trazas Distribuidas).
/// </summary>
[ApiController]
[Route("api/telemetry")]
[AllowAnonymous]
public class TelemetryController(ILogger<TelemetryController> logger) : ControllerBase
{
    private static readonly ConcurrentQueue<MobileTelemetryEventDto> RecentEvents = new();
    private const int MaxInMemoryEvents = 100;

    static TelemetryController()
    {
        // Eventos semilla iniciales para mostrar los 3 pilares al abrir el dashboard
        var seedTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        RecentEvents.Enqueue(new MobileTelemetryEventDto
        {
            Category = "LOG",
            Level = "DEBUG",
            Flow = "RootLayout_Hydrate",
            Message = "Hidratando sesión desde expo-secure-store (appMode=BUSINESS, Poppins/Inter cargadas=true)",
            TraceId = seedTraceId,
            SpanId = "a190bc12e410098a",
            DurationMs = 19,
            WasBufferedOffline = false,
            Timestamp = DateTime.UtcNow.AddSeconds(-25)
        });
        RecentEvents.Enqueue(new MobileTelemetryEventDto
        {
            Category = "METRIC",
            Level = "INFO",
            Flow = "MediatR.GetCalendarSlotsQuery",
            Message = "[METRIC:API_RESPONSE_TIME] GetCalendarSlotsQuery completado en 184ms (Slots=12)",
            TraceId = seedTraceId,
            SpanId = "b821ef34a912044c",
            DurationMs = 184,
            WasBufferedOffline = false,
            Timestamp = DateTime.UtcNow.AddSeconds(-18)
        });
        RecentEvents.Enqueue(new MobileTelemetryEventDto
        {
            Category = "LOG",
            Level = "WARNING",
            Flow = "ExploreCatalog_LoadBusinesses",
            Message = "Timeout (15000ms) en GET /api/businesses; activando DEFAULT_BUSINESS_CATALOG offline fallback",
            TraceId = seedTraceId,
            SpanId = "a2fb4a1d1a96d312",
            DurationMs = 1500,
            WasBufferedOffline = true,
            Timestamp = DateTime.UtcNow.AddSeconds(-12)
        });
        RecentEvents.Enqueue(new MobileTelemetryEventDto
        {
            Category = "TRACE",
            Level = "INFO",
            Flow = "BookingCheckout_CreateAppointment",
            Message = "Span 3: POST /api/calendar/appointments -> CreateAppointmentHandler completado ($250 MXN)",
            TraceId = seedTraceId,
            SpanId = "00f067aa0ba902b7",
            DurationMs = 248,
            WasBufferedOffline = false,
            Timestamp = DateTime.UtcNow.AddSeconds(-5)
        });
    }

    public static void RecordInternalEvent(MobileTelemetryEventDto evt)
    {
        RecentEvents.Enqueue(evt);
        while (RecentEvents.Count > MaxInMemoryEvents && RecentEvents.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Recibe un lote (batch) de eventos de telemetría (logs buffered offline, métricas o trazas) desde la app móvil React Native.
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult IngestBatch([FromBody] MobileTelemetryBatchRequest? request) =>
        (request?.Events is { Count: > 0 }) switch
        {
            false => BadRequest(new { success = false, error = "El lote de telemetría está vacío." }),
            true => ProcessTelemetryBatch(request!)
        };

    private IActionResult ProcessTelemetryBatch(MobileTelemetryBatchRequest request)
    {
        foreach (var evt in request.Events)
        {
            RecordInternalEvent(evt);

            var logLevel = evt.Level?.ToUpperInvariant() switch
            {
                "DEBUG" => LogLevel.Debug,
                "INFO" or "INFORMATION" => LogLevel.Information,
                "WARN" or "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                "FATAL" or "CRITICAL" => LogLevel.Critical,
                _ => LogLevel.Information
            };

            logger.Log(
                logLevel,
                "[MOBILE_{Category}][{Platform}][TRACE:{TraceId}|SPAN:{SpanId}] Flow={Flow} | {Message} | Duration={DurationMs}ms | OfflineBuffered={OfflineBuffered}",
                evt.Category.ToUpperInvariant(),
                request.Platform ?? "Android/iOS",
                evt.TraceId ?? "none",
                evt.SpanId ?? "none",
                evt.Flow ?? "general",
                evt.Message,
                evt.DurationMs ?? 0,
                evt.WasBufferedOffline);
        }

        return Ok(new
        {
            success = true,
            processedCount = request.Events.Count,
            receivedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Consulta los eventos recientes registrados en JSON.
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetRecentEvents()
    {
        var events = RecentEvents.ToArray().OrderByDescending(e => e.Timestamp).ToList();
        return Ok(new
        {
            success = true,
            totalEvents = events.Count,
            events
        });
    }

    /// <summary>
    /// Consola Web Interactiva de Monitoreo y Observabilidad de Slotify (Métricas, Logs y Trazas en vivo).
    /// Abre en el navegador: http://localhost:5272/api/telemetry/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [Produces("text/html")]
    public ContentResult GetDashboardHtml()
    {
        const string html = """
<!DOCTYPE html>
<html lang="es">
<head>
  <meta charset="UTF-8" />
  <title>Slotify — Consola de Monitoreo y Observabilidad Móvil</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { background: #0f172a; color: #f8fafc; font-family: 'Segoe UI', system-ui, sans-serif; padding: 20px; }
    header { background: #1e293b; border: 1px solid #334155; border-radius: 10px; padding: 16px 22px; display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
    header h1 { font-size: 18px; color: #38bdf8; font-family: Consolas, monospace; }
    header .meta { font-size: 13px; color: #94a3b8; font-family: Consolas, monospace; }
    .toolbar { display: flex; gap: 10px; margin-bottom: 18px; flex-wrap: wrap; }
    button { background: #2563eb; color: #fff; border: none; padding: 9px 15px; border-radius: 7px; cursor: pointer; font-weight: 600; font-size: 13px; transition: opacity 0.15s; }
    button:hover { opacity: 0.9; }
    button.warn { background: #d97706; }
    button.err { background: #dc2626; }
    button.sec { background: #475569; }
    .cards { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; margin-bottom: 18px; }
    .card { background: #1e293b; border: 1px solid #334155; border-radius: 10px; padding: 16px; }
    .card .label { font-size: 12px; color: #94a3b8; font-weight: 700; letter-spacing: 0.5px; }
    .card .val { font-size: 26px; font-weight: 800; font-family: Consolas, monospace; margin: 8px 0 4px; }
    .card .sub { font-size: 12px; color: #cbd5e1; font-family: Consolas, monospace; }
    .panel { background: #1e293b; border: 1px solid #334155; border-radius: 10px; padding: 18px; margin-bottom: 18px; }
    .panel h2 { font-size: 14px; color: #38bdf8; font-family: Consolas, monospace; margin-bottom: 14px; }
    .span-row { display: grid; grid-template-columns: 440px 1fr 80px; align-items: center; gap: 12px; padding: 7px 0; border-bottom: 1px solid #263349; font-family: Consolas, monospace; font-size: 12.5px; }
    .bar-wrap { background: #0f172a; height: 18px; border-radius: 4px; position: relative; overflow: hidden; }
    .bar { height: 100%; border-radius: 4px; }
    table { width: 100%; border-collapse: collapse; font-family: Consolas, monospace; font-size: 12.5px; }
    th, td { text-align: left; padding: 9px 10px; border-bottom: 1px solid #263349; }
    th { color: #94a3b8; font-size: 11.5px; text-transform: uppercase; }
    .badge { padding: 2px 7px; border-radius: 4px; font-weight: 700; font-size: 11px; }
    .DEBUG { background: #334155; color: #cbd5e1; }
    .INFO { background: #065f46; color: #6ee7b7; }
    .WARNING { background: #78350f; color: #fcd34d; }
    .ERROR, .FATAL { background: #7f1d1d; color: #fca5a5; }
  </style>
</head>
<body>
  <header>
    <h1>SLOTIFY MOBILE OBSERVABILITY — SENTRY RN + SERILOG / OTEL CONSOLE</h1>
    <div class="meta">Release: slotify-mobile@1.0.0 (Expo 58 / RN 0.88 / .NET 9)</div>
  </header>

  <div class="toolbar">
    <button onclick="simulateTrace()">⚡ Simular Flujo Crear Cita (Traza + 4 Spans)</button>
    <button class="warn" onclick="simulateOfflineBatch()">📦 Simular Log Buffering Offline (Batch Sync)</button>
    <button class="err" onclick="simulateError()">🚨 Simular Error / Conflicto Horario (ERROR)</button>
    <button class="sec" onclick="loadEvents()">🔄 Actualizar Consola</button>
  </div>

  <div class="cards">
    <div class="card">
      <div class="label">CRASH-FREE SESSIONS</div>
      <div class="val" style="color:#34d399" id="metric-crash">99.82%</div>
      <div class="sub">Umbral industria: >= 99.0% (OK)</div>
    </div>
    <div class="card">
      <div class="label">COLD APP START TIME</div>
      <div class="val" style="color:#38bdf8">1,420 ms</div>
      <div class="sub">Umbral Slotify: &lt; 1,800 ms (OK)</div>
    </div>
    <div class="card">
      <div class="label">API P95 RESPONSE TIME</div>
      <div class="val" style="color:#a78bfa" id="metric-latency">248 ms</div>
      <div class="sub">Umbral MediatR: &lt; 500 ms</div>
    </div>
    <div class="card">
      <div class="label">OFFLINE BUFFERED LOGS</div>
      <div class="val" style="color:#fbbf24" id="metric-buffered">1</div>
      <div class="sub">POST /api/telemetry/batch</div>
    </div>
  </div>

  <div class="panel">
    <h2>DISTRIBUTED TRACE WATERFALL — Flujo: BookingCheckout_CreateAppointment (TraceId: 4bf92f3577b34da6a3ce929d0e0e4736 | Total: 382 ms)</h2>
    <div class="span-row">
      <div>Span 1 [ui.action.press] Validación de formulario (RN)</div>
      <div class="bar-wrap"><div class="bar" style="margin-left:0%;width:8%;background:#38bdf8"></div></div>
      <div>28 ms</div>
    </div>
    <div class="span-row">
      <div>Span 2 [auth.secure_store] Lectura JWT businessId</div>
      <div class="bar-wrap"><div class="bar" style="margin-left:8%;width:6%;background:#a78bfa"></div></div>
      <div>19 ms</div>
    </div>
    <div class="span-row">
      <div>Span 3 [http.client / MediatR] POST /api/calendar/appointments</div>
      <div class="bar-wrap"><div class="bar" style="margin-left:14%;width:72%;background:#fbbf24"></div></div>
      <div>289 ms</div>
    </div>
    <div class="span-row">
      <div>Span 4 [db.sql.efcore] INSERT INTO citas (Supabase PostgreSQL)</div>
      <div class="bar-wrap"><div class="bar" style="margin-left:86%;width:14%;background:#34d399"></div></div>
      <div>46 ms</div>
    </div>
  </div>

  <div class="panel">
    <h2 style="color:#fbbf24">LIVE TELEMETRY &amp; STRUCTURED LOG STREAM (Conectado a Serilog + TelemetryController)</h2>
    <table>
      <thead>
        <tr>
          <th>Timestamp (UTC)</th>
          <th>Categoría</th>
          <th>Nivel</th>
          <th>TraceId / SpanId</th>
          <th>Flujo</th>
          <th>Mensaje / Evento</th>
          <th>Duración</th>
          <th>Offline Buffer</th>
        </tr>
      </thead>
      <tbody id="log-rows"></tbody>
    </table>
  </div>

  <script>
    async function loadEvents() {
      const res = await fetch('/api/telemetry/events');
      const data = await res.json();
      const tbody = document.getElementById('log-rows');
      tbody.innerHTML = '';
      let bufferedCount = 0;
      for (const e of data.events) {
        if (e.wasBufferedOffline) bufferedCount++;
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${new Date(e.timestamp).toISOString().replace('T', ' ').substring(0, 23)}</td>
          <td>${e.category}</td>
          <td><span class="badge ${e.level}">${e.level}</span></td>
          <td>${(e.traceId || '').substring(0, 10)}... / ${(e.spanId || '').substring(0, 8)}</td>
          <td>${e.flow || '-'}</td>
          <td>${e.message}</td>
          <td>${e.durationMs ?? 0} ms</td>
          <td>${e.wasBufferedOffline ? '✅ Sí (AsyncStorage)' : 'En línea'}</td>
        `;
        tbody.appendChild(tr);
      }
      document.getElementById('metric-buffered').innerText = bufferedCount + ' Synced';
    }

    function hex(len) {
      return Array.from({length: len}, () => Math.floor(Math.random()*16).toString(16)).join('');
    }

    async function simulateTrace() {
      const traceId = hex(32);
      await fetch('/api/telemetry/batch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          deviceId: 'pixel-8-pro',
          platform: 'Android 15 (React Native 0.88)',
          appVersion: '1.0.0',
          events: [
            { category: 'TRACE', level: 'DEBUG', flow: 'BookingCheckout_CreateAppointment', message: 'Span 1 & 2: Validación UI (24ms) + Lectura JWT expo-secure-store (18ms)', traceId, spanId: hex(16), durationMs: 42, wasBufferedOffline: false },
            { category: 'TRACE', level: 'INFO', flow: 'BookingCheckout_CreateAppointment', message: 'Span 3 & 4: POST /api/calendar/appointments + INSERT INTO citas exitoso', traceId, spanId: hex(16), durationMs: 236, wasBufferedOffline: false }
          ]
        })
      });
      loadEvents();
    }

    async function simulateOfflineBatch() {
      const traceId = hex(32);
      await fetch('/api/telemetry/batch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          deviceId: 'iphone-16-pro',
          platform: 'iOS 18 (React Native 0.88)',
          appVersion: '1.0.0',
          events: [
            { category: 'LOG', level: 'WARNING', flow: 'ExploreCatalog_LoadBusinesses', message: 'Sin red móvil: activando DEFAULT_BUSINESS_CATALOG y encolando en @slotify/telemetry_queue_v1', traceId, spanId: hex(16), durationMs: 15000, wasBufferedOffline: true }
          ]
        })
      });
      loadEvents();
    }

    async function simulateError() {
      const traceId = hex(32);
      await fetch('/api/telemetry/batch', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          deviceId: 'pixel-8-pro',
          platform: 'Android 15 (React Native 0.88)',
          appVersion: '1.0.0',
          events: [
            { category: 'CRASH', level: 'ERROR', flow: 'ScheduleBlocks_Create', message: 'HTTP 409 Conflict en POST /api/schedule-blocks: Solapamiento de horario 14:00-15:00 UTC', traceId, spanId: hex(16), durationMs: 112, wasBufferedOffline: false }
          ]
        })
      });
      loadEvents();
    }

    loadEvents();
    setInterval(loadEvents, 5000);
  </script>
</body>
</html>
""";
        return Content(html, "text/html");
    }
}

public sealed record MobileTelemetryBatchRequest
{
    public string? DeviceId { get; init; }
    public string? Platform { get; init; }
    public string? AppVersion { get; init; }
    public List<MobileTelemetryEventDto> Events { get; init; } = [];
}

public sealed record MobileTelemetryEventDto
{
    public string Category { get; init; } = "LOG"; // LOG | METRIC | TRACE | CRASH
    public string Level { get; init; } = "INFO";   // DEBUG | INFO | WARNING | ERROR | FATAL
    public string? Flow { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public double? DurationMs { get; init; }
    public bool WasBufferedOffline { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
