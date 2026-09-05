#region
using Asset.Application.Behaviors;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.IService;
using Asset.Application.Features.AI.ServiceImplementation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
#endregion

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

        // Validations
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Caching
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        // AI
        services.AddScoped<IAssetQuestionParserService, RuleBasedAssetQuestionParser>();
        return services;
    }
}