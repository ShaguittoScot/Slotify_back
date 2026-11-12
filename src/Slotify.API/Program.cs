using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Slotify.API.Common;
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
            ?? throw new InvalidOperationException("Jwt:Secret not configured");

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
    });

builder.Services.AddAuthorization();

// API services
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// CORS for React Native
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
                ADD COLUMN IF NOT EXISTS citas_historial UUID[] NOT NULL DEFAULT '{}'::UUID[];
            """);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply auto-migration checks (US-017 / US-026)");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();

public partial class Program { }
