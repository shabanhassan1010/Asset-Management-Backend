#region
using Asset.API.Extensions;
using Asset.API.Middleware;
using Asset.Application;
using Asset.Application.Interfaces.Comman;
using Asset.Infastructure;
using Asset.Infastructure.DBContext;
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
                              .AllowAnyHeader()     // allow for amgular client to send any headers (like Authorization)
                              .AllowAnyMethod()     // allow for angular client to send any HTTP method (GET, POST, PUT, DELETE, etc.)
                              .AllowCredentials();  // allow for angular client to send cookies (like .AspNetCore.Identity.Application)
                    });
                });
                #endregion

                #region Localization
                // (1) Registers IStringLocalizer<T>.
                //     No ResourcesPath on purpose: SharedResources lives in namespace
                //     Asset.Application.Resources, inside the Resources folder, so its .resx files are
                //     found by the class's own namespace. Adding ResourcesPath = "Resources" here would
                //     make .NET look for "Asset.Application.Resources.Resources.SharedResources" (twice) and
                //     every lookup would silently fall back to the key.
                builder.Services.AddLocalization();

                // (2) Which languages a request may use. English is the default: a request with no
                //     Accept-Language header, or with a language we don't support, gets English.
                //     The cache stays correct with two languages because LocalizationBehavior
                //     translates AFTER the cache (see Asset.Application/Behaviors/LocalizationBehavior.cs).
                //     Two different lists on purpose:
                //       UICulture = the language of the TEXT (messages)        → en or ar
                //       Culture   = how numbers and dates are FORMATTED on the server → always en
                //     (the "ar" culture can switch dates to the Hijri calendar; the Angular app
                //      formats dates for the user, the server never needs to.)
                var supportedUICultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ar")
                };

                var localizationOptions = new RequestLocalizationOptions
                {
                    DefaultRequestCulture = new RequestCulture("en"),
                    SupportedCultures = new[] { new CultureInfo("en") },
                    SupportedUICultures = supportedUICultures,
                    // "ar-EG" or "ar-SA" from the browser still selects "ar".
                    FallBackToParentCultures = true,
                    FallBackToParentUICultures = true
                };

                // Only the Accept-Language header (sent by the Angular languageInterceptor).
                // The default providers also read ?culture= and a cookie; one source is easier to reason about.
                localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new AcceptLanguageHeaderRequestCultureProvider()
                };
                #endregion

                builder.Services.AddControllers();
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();

                await IdentitySeeder.SeedAsync(app.Services);

                #region Middleware
                // FIRST: set the request's language from Accept-Language.
                // It must run BEFORE ExceptionHandlingMiddleware: the culture only flows "down" the
                // pipeline, so an error handler placed before it would always answer in English.
                app.UseRequestLocalization(localizationOptions);

                app.UseMiddleware<ExceptionHandlingMiddleware>();
                #endregion

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }


                app.UseHttpsRedirection();

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