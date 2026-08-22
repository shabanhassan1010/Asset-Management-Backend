using Asset.Application.Behaviors;
using Asset.Application.Interfaces.Comman;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Asset.Application;
public static class ModuleApplicationDependencies
{
    public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
    {
        var assembly = typeof(ModuleApplicationDependencies).Assembly;

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(assembly);
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Caching
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        return services;
    }
}