#region
using Asset.API.Extensions;
using Asset.API.Middleware;
using Asset.API.Services;
using Asset.Application;
using Asset.Application.Common.Interfaces;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.ServiceImplementation;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Enum;
using Asset.Infastructure;
using Asset.Infastructure.DBContext.Identity;
using Asset.Infastructure.Service;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
#endregion

namespace Asset.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const string AngularCorsPolicy = "AngularClient";
            var builder = WebApplication.CreateBuilder(args);

            #region Dependency Injection
            builder.Services.AddInfrastructureDependencies(builder.Configuration);
            builder.Services.AddCoreDependencies();
            builder.Services.AddScoped<ICurrentUser, CurrentUser>();
            builder.Services.AddAiRateLimiting();
            #endregion

            #region CORS 
            builder.Services.AddCors(options =>
            {

                options.AddPolicy(AngularCorsPolicy, policy =>
                {
                    // origin الأنجولار بييجي من appsettings، مش هارد كودد —
                    // مختلف بين Development و Production.
                    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();   // لو الـ JWT بيتبعت كـ header عادي مش محتاجها، لكن سيبها لو فيه refresh cookie
                });
            });
            #endregion

            #region Localization
            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            #endregion

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            await IdentitySeeder.SeedAsync(app.Services);

            #region Localization Configuration
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar")
            };

            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };

            #endregion

            #region Middleware
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            #endregion

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseHttpsRedirection();

            app.UseRequestLocalization(localizationOptions);

            app.UseCors(AngularCorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapControllers();

            app.Run();
        }
    }
}
