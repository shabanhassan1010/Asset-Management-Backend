#region
using Asset.API.Extensions;
using Asset.API.Middleware;
using Asset.Application;
using Asset.Application.Interfaces.Comman;
using Asset.Infastructure;
using Asset.Infastructure.DBContext.Identity;
using Asset.Infastructure.Service;
using Microsoft.AspNetCore.Localization;
using Serilog;
using System.Globalization;
#endregion

namespace Asset.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // (A) Temporary logger: works until the app is fully built
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
            try
            {
                Log.Information("Starting Asset.API");

                const string AngularCorsPolicy = "AngularClient";
                var builder = WebApplication.CreateBuilder(args);

                // (B) Replace the default logger with Serilog, read settings from appsettings.json
                builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

                #region Dependency Injection
                builder.Services.AddInfrastructureDependencies(builder.Configuration);
                builder.Services.AddApplicationDependencies();

                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddAiRateLimiting();
                #endregion

                #region CORS 
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy(AngularCorsPolicy, policy =>
                    {
                        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
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
            catch (Exception ex)                                   // [Serilog]
            {
                // (D) Any crash during startup (DI, seeding, config) is logged here
                Log.Fatal(ex, "Asset.API failed to start");
            }
            finally                                                // [Serilog]
            {
                // (E) Make sure the last logs are written to the file before the app closes
                Log.CloseAndFlush();
            }                    
        }
    }
}