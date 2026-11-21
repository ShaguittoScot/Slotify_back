using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Slotify.API.Common;
using Slotify.API.Middleware;
using Slotify.API.Services;
using Slotify.Application;
using Slotify.Application.Common.Interfaces;
using Slotify.Infrastructure;
using System.Text;

// Load .env file from solution/project root if present
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Clean Architecture layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpClient<ISupabaseIdentityVerifier, SupabaseIdentityVerifier>(client =>
    client.Timeout = TimeSpan.FromSeconds(8))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IObservabilityService, ObservabilityService>();

// Auth Setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var jwtSecret = configuration["Jwt:Secret"]
            ?? configuration["JWT_SECRET"];

        if (string.IsNullOrWhiteSpace(jwtSecret) ||
            string.Equals(jwtSecret, "YOUR_JWT_SECRET_KEY_MIN_32_CHARS_LONG", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Jwt:Secret is not configured.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidIssuer = Slotify.Infrastructure.Services.JwtService.Issuer,
            ValidAudience = Slotify.Infrastructure.Services.JwtService.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments("/api/auth"))
                {
                    // Los endpoints /api/auth usan el token de Supabase y lo verifican con ISupabaseIdentityVerifier
                    context.NoResult();
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// API services
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var configuredOrigins = (builder.Configuration["Cors:AllowedOrigins"]
    ?? builder.Configuration["CORS_ALLOWED_ORIGINS"]
    ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SlotifyCors", policy =>
    {
        if (configuredOrigins.Length > 0)
        {
            policy.WithOrigins(configuredOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Safe auto-migration for US-017 & US-026
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Slotify.Infrastructure.Data.AppDbContext>();
    try
    {
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
            db.Database,
            """
            ALTER TABLE negocios ADD COLUMN IF NOT EXISTS configuracion_formulario_personalizada JSONB;
            ALTER TABLE negocios ADD COLUMN IF NOT EXISTS onboarding_completed_at TIMESTAMPTZ NULL;
            CREATE TABLE IF NOT EXISTS public.clientes_negocio (
                id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                id_negocio UUID NOT NULL REFERENCES public.negocios(id) ON DELETE CASCADE,
                id_cliente UUID REFERENCES public.clientes(id) ON DELETE SET NULL,
                nombre VARCHAR(100) NOT NULL,
                apellido VARCHAR(100) NOT NULL DEFAULT '',
                correo VARCHAR(150) NOT NULL DEFAULT '',
                telefono VARCHAR(30),
                notas VARCHAR(1000),
                fecha_creacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                fecha_actualizacion TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_clientes_negocio_negocio ON public.clientes_negocio (id_negocio, id);
            CREATE INDEX IF NOT EXISTS idx_clientes_negocio_negocio_cliente ON public.clientes_negocio (id_negocio, id_cliente);
            ALTER TABLE public.clientes_negocio
                ADD COLUMN IF NOT EXISTS fecha_archivado TIMESTAMP WITH TIME ZONE,
                ADD COLUMN IF NOT EXISTS citas_historial UUID[] NOT NULL DEFAULT ARRAY[]::UUID[];
            """);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply auto-migration checks (US-017 / US-026)");
    }

    try
    {
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
            db.Database,
            """
            INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
            VALUES (
                'business-logos',
                'business-logos',
                TRUE,
                4194304,
                ARRAY['image/jpeg', 'image/png', 'image/webp']
            )
            ON CONFLICT (id) DO UPDATE
            SET public = TRUE,
                file_size_limit = 4194304,
                allowed_mime_types = ARRAY['image/jpeg', 'image/png', 'image/webp'];
            """);
        Log.Information("Supabase Storage bucket 'business-logos' verified.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not auto-create Supabase Storage bucket 'business-logos'; run supabase/storage_business_logos.sql in Supabase SQL Editor.");
    }

    try
    {
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(
            db.Database,
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Public read business-logos'
                ) THEN
                    CREATE POLICY "Public read business-logos"
                    ON storage.objects FOR SELECT
                    TO public
                    USING (bucket_id = 'business-logos');
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated upload own folder in business-logos'
                ) THEN
                    CREATE POLICY "Authenticated upload own folder in business-logos"
                    ON storage.objects FOR INSERT
                    TO authenticated
                    WITH CHECK (
                        bucket_id = 'business-logos'
                        AND (storage.foldername(name))[1] = auth.uid()::text
                    );
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated update own folder in business-logos'
                ) THEN
                    CREATE POLICY "Authenticated update own folder in business-logos"
                    ON storage.objects FOR UPDATE
                    TO authenticated
                    USING (
                        bucket_id = 'business-logos'
                        AND (storage.foldername(name))[1] = auth.uid()::text
                    );
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE schemaname = 'storage' AND tablename = 'objects' AND policyname = 'Authenticated delete own folder in business-logos'
                ) THEN
                    CREATE POLICY "Authenticated delete own folder in business-logos"
                    ON storage.objects FOR DELETE
                    TO authenticated
                    USING (
                        bucket_id = 'business-logos'
                        AND (storage.foldername(name))[1] = auth.uid()::text
                    );
                END IF;
            END $$;
            """);
        Log.Information("Supabase Storage RLS policies for 'business-logos' verified.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not auto-create storage.objects RLS policies; run supabase/storage_business_logos.sql in Supabase SQL Editor.");
    }
}

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors("SlotifyCors");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Portal web estático de reservas públicas (/book y /book/{slug})
string ResolveBookingPortalFile(string fileName)
{
    var siblingPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "..", "Slotify", "web-booking", fileName));
    if (File.Exists(siblingPath)) return siblingPath;
    return Path.Combine(app.Environment.ContentRootPath, "wwwroot", "book", fileName);
}

IResult ServeBookingPortalFile(string fileName, string contentType)
{
    var filePath = ResolveBookingPortalFile(fileName);
    return File.Exists(filePath)
        ? Results.File(filePath, contentType)
        : Results.NotFound();
}

app.MapGet("/book", (HttpContext ctx) =>
    ctx.Request.Path.Value?.EndsWith('/') == true
        ? ServeBookingPortalFile("index.html", "text/html; charset=utf-8")
        : Results.Redirect("/book/")).AllowAnonymous();
app.MapGet("/book/styles.css", () => ServeBookingPortalFile("styles.css", "text/css; charset=utf-8")).AllowAnonymous();
app.MapGet("/book/app.js", () => ServeBookingPortalFile("app.js", "application/javascript; charset=utf-8")).AllowAnonymous();
app.MapGet("/book/{slug}", () => ServeBookingPortalFile("index.html", "text/html; charset=utf-8")).AllowAnonymous();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();

public partial class Program { }
