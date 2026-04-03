using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Oravity.Infrastructure.Audit;
using Oravity.Infrastructure.Cache;
using Oravity.Infrastructure.Database;
using Oravity.Infrastructure.Messaging;
using Oravity.Infrastructure.Services;
using Oravity.Infrastructure.Tenancy;
using Oravity.SharedKernel.Interfaces;
using StackExchange.Redis;

namespace Oravity.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // AuditInterceptor — DbContext'e eklenecek, scoped olmalı
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<AuditLogService>();

        // PostgreSQL / EF Core — AuditInterceptor'ı AddInterceptors ile bağla
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            var interceptor = sp.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(interceptor);
        });

        // Redis — abortConnect=false: bağlantı yoksa exception fırlatmak yerine yeniden dener
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisOptions));
        services.AddScoped<ICacheService, RedisCacheService>();

        // MediatR Event Bus
        services.AddScoped<IEventBus, MediatREventBus>();

        // Database Seeder
        services.AddDatabaseSeeder();

        // Tenant Context (scoped — request bazlı)
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // Encryption Service
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // HttpContextAccessor (CurrentUserService için)
        services.AddHttpContextAccessor();

        // CurrentUser Service
        services.AddScoped<ICurrentUser, CurrentUserService>();

        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret ayarı eksik. appsettings.json dosyasını kontrol edin.");

        var portalSecret = configuration["Jwt:PortalSecret"]
            ?? throw new InvalidOperationException("Jwt:PortalSecret ayarı eksik.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            // Klinik personel JWT — varsayılan scheme
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            })
            // Hasta portalı JWT — ayrı scheme + ayrı secret
            .AddJwtBearer("PatientPortal", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(portalSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection")!)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = ["default", "critical", "low"];
        });

        return services;
    }
}
