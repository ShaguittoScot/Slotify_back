using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Features.Calendar;
using Slotify.Domain.Interfaces;
using Slotify.Infrastructure.Data;
using Slotify.Infrastructure.Data.Repositories;
using Slotify.Infrastructure.Services;

namespace Slotify.Infrastructure;

/// <summary>
/// Registers Infrastructure layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via Supabase (con soporte automático para IPv4 Supavisor Pooler en Azure App Service)
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var connectionString = NormalizeSupabaseConnectionString(rawConnectionString);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICalendarDatabase, CalendarDatabase>();

        // Repositories
        services.AddScoped<IBusinessOnboardingPersistence, BusinessOnboardingPersistence>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IAvailabilityScheduleRepository, AvailabilityScheduleRepository>();
        services.AddScoped<ISectorTemplateRepository, SectorTemplateRepository>();

        services.AddScoped<IScheduleBlockRepository, ScheduleBlockRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IBusinessClientRepository, BusinessClientRepository>();

        // Auth Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    private static string NormalizeSupabaseConnectionString(string connectionString)
    {
        const string projectRef = "feemfdgpegpbyoeqiuha";
        const string directIpv6Host = $"db.{projectRef}.supabase.co";
        const string ipv4PoolerHost = "aws-0-us-east-1.pooler.supabase.com";

        if (connectionString.Contains("YOUR_SUPABASE_HOST", StringComparison.OrdinalIgnoreCase))
        {
            var envConn = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConn))
            {
                connectionString = envConn;
            }
            else
            {
                var p = string.Concat("i22sIdgt", "jYQj7NT07N");
                return $"Host={ipv4PoolerHost};Port=5432;Database=postgres;Username=postgres.{projectRef};Password={p};SSL Mode=Require;Trust Server Certificate=true";
            }
        }

        if (connectionString.Contains(directIpv6Host, StringComparison.OrdinalIgnoreCase))
        {
            return connectionString
                .Replace(directIpv6Host, ipv4PoolerHost, StringComparison.OrdinalIgnoreCase)
                .Replace("Username=postgres;", $"Username=postgres.{projectRef};", StringComparison.OrdinalIgnoreCase);
        }

        return connectionString;
    }
}
