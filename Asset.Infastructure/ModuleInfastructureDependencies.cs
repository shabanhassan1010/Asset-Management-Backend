#region
using Asset.Application.Common.Caching;
using Asset.Application.Common.Interfaces;
using Asset.Application.Features.AI.Interfases;
using Asset.Application.Features.AI.ServiceImplementation;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Application.Interfaces.Repository;
using Asset.Domain.Identity;
using Asset.Infastructure.DBContext.Identity;
using Asset.Infastructure.Models;
using Asset.Infastructure.Repositories;
using Asset.Infastructure.Security;
using Asset.Infastructure.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
#endregion
namespace Asset.Infastructure
{
    public static class ModuleInfastructureDependencies
    {
        public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AssetManagementDbContext>(options => options.UseSqlServer(connectionString));
            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(connectionString));
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Password
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                // Email
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>();

            #region Authentication
            var signingKey = configuration["Jwt:SigningKey"]
                ?? throw new InvalidOperationException("Jwt:SigningKey is missing. Run: dotnet user-secrets set \"Jwt:SigningKey\" \"<value>\"");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                            ClockSkew = TimeSpan.Zero,
                            RoleClaimType = ClaimTypes.Role,      
                            NameClaimType = ClaimTypes.NameIdentifier
                        };
                    });
            #endregion

            #region Repositories
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAssetRepository, AssetRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IAssetTransferRepository, AssetTransferRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IAssetTypeRepository, AssetTypeRepository>();
            #endregion

            #region Identity and tokens
            services.Configure<JWTSettings>(configuration.GetSection(JWTSettings.SectionName));
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ITokenHasher, TokenHasher>();
            #endregion

            #region Caching (Redis)

            // Registers IDistributedCache backed by Redis.
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                // Prefix on every key, so this app's keys are distinguishable
                // from any other app sharing the same Redis instance.
                options.InstanceName = "asset:";
            });
            // The Application layer depends on ICacheService, never on IDistributedCache.
            services.AddScoped<ICacheService, RedisCacheService>();

            #endregion

            #region AI assistant
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IAssetQuestionParser, RuleBasedAssetQuestionParser>();
            services.AddScoped<IAiLookupRepository, AiLookupRepository>();
            #endregion

            return services;
        }
    }
}