using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MultiTenantSaaS.Application.Common.Behaviors;
using MultiTenantSaaS.Application.Services;

namespace MultiTenantSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Add chartering services
        services.AddScoped<ICharteringCalculationService, CharteringCalculationService>();
        services.AddScoped<IEstimationValidationService, EstimationValidationService>();
        services.AddScoped<IBunkerRobService, BunkerRobService>();
        services.AddScoped<ICalculationDetailsService, CalculationDetailsService>();

        return services;
    }
}
