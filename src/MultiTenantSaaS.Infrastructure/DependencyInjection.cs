using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MultiTenantSaaS.Application.Common.Interfaces;
using MultiTenantSaaS.Domain.Entities;
using MultiTenantSaaS.Infrastructure.Identity;
using MultiTenantSaaS.Infrastructure.Persistence;
using MultiTenantSaaS.Infrastructure.Services;
using MultiTenantSaaS.Infrastructure.Services.Assistant;
using MultiTenantSaaS.Infrastructure.Services.Optimization;

namespace MultiTenantSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Database Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=MultiTenantSaaSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // 1b. Performance module — its own, physically separate database/connection.
        var performanceConnectionString = configuration.GetConnectionString("PerformanceConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=MultiTenantSaaS_PerformanceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<PerformanceDbContext>((sp, options) =>
        {
            options.UseSqlServer(performanceConnectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(PerformanceDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IPerformanceDbContext>(provider => provider.GetRequiredService<PerformanceDbContext>());

        // 2. Identity Configuration
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. JWT Authentication Configuration
        var secretKey = configuration["Jwt:SecretKey"] ?? "SuperSecretEnterpriseJwtSigningKeyWithSufficientLengthForHS256Bit!";
        var issuer = configuration["Jwt:Issuer"] ?? "MultiTenantSaaS";
        var audience = configuration["Jwt:Audience"] ?? "MultiTenantSaaS.Client";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // 4. Register Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddHttpClient();
        services.AddSingleton<ICycloneDataService, CycloneDataService>();
        services.AddSingleton<IMaritimeRegionService, MaritimeRegionService>();
        services.AddScoped<IWeatherDataService, WeatherDataService>();
        services.AddScoped<IRouteOptimizationEngine, RouteOptimizationEngine>();
        services.AddSingleton<IOptimizationJobService, OptimizationJobService>();
        services.AddHostedService<OptimizationJobRecoveryService>();
        services.AddScoped<IOptimizationMonitoringService, OptimizationMonitoringService>();
        services.AddScoped<IEnsembleWindService, OpenMeteoEnsembleWindService>();
        services.AddScoped<IEtsPolicyService, EtsPolicyService>();
        services.AddScoped<IPerformanceCalibrationService, PerformanceCalibrationService>();

        // AI assistant tools: every IAssistantTool in this assembly is picked up automatically.
        services.AddMemoryCache();
        foreach (var toolType in typeof(AssistantToolRegistry).Assembly.GetTypes()
                     .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IAssistantTool).IsAssignableFrom(t)))
            services.AddScoped(typeof(IAssistantTool), toolType);
        services.AddScoped<IAssistantToolRegistry, AssistantToolRegistry>();

        return services;
    }
}
