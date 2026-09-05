#region
using Asset.Application.Behaviors;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.ServiceImplementation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
#endregion

namespace Asset.Application;
public static class ModuleApplicationDependencies
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        // assembly of the current project (Asset.Application) to register MediatR, AutoMapper, and FluentValidation
        // and it use in complie time to find all the handlers, mappings, and validators in the assembly
        // and also deticate any error in compile time if any handler, mapping, or validator is missing or not implemented correctly.
        var assembly = typeof(ModuleApplicationDependencies).Assembly;

        // MediatR : only Use it in the Application Layer, register all the handlers in the assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Validations : Only Use it in the Application Layer.
            // responsible for executing the validation, and it will be executed before the handler is executed.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // Caching : Application layer depends on ICacheService, never on IDistributedCache.
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        // AutoMapper : Any Mapping exists in the Application Layer
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(assembly);
        });

        // FluentValidation : The Validators related into Command and Query exist in the Application Layer Only.
        // this line just register all the validators in the assembly, but he is not responsible for executing the validation.
        services.AddValidatorsFromAssembly(assembly);

        // AI 
        services.AddScoped<IAssetQuestionParserService, RuleBasedAssetQuestionParser>();
        return services;
    }
}

#region Execution Flow of a Request in the Application Layer
//               Request
//                  ↓
//           ValidationBehavior      
//                  ↓
//            CachingBehavior         
//                  ↓
//               Handler                 
//                  ↓
//               Response
#endregion