using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Slotify.Application.Common.Behaviors;

namespace Slotify.Application;

/// <summary>
/// Registers Application layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR + handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation - auto-register all validators
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
