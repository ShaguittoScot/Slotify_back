using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slotify.Application.Common.Interfaces;
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
        // PostgreSQL via Supabase
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();

        // Auth Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
