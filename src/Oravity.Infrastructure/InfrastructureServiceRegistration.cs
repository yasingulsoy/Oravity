using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Oravity.Infrastructure.Audit;
using Oravity.Infrastructure.Cache;
using Oravity.Infrastructure.Database;
using Oravity.Infrastructure.Messaging;
using Oravity.Infrastructure.Services;
using Oravity.Infrastructure.Storage;
using Oravity.Infrastructure.Tenancy;
using Minio;
using Oravity.SharedKernel.Interfaces;
using Oravity.SharedKernel.Services;
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

        // SSO callback (Microsoft OIDC tamamlandığında)
        services.AddScoped<SsoCallbackHandler>();

        // Fiyatlandırma Motoru
        services.AddSingleton<FormulaEngine>();
        services.AddSingleton<PricingEngine>();

        // Döviz Kuru Servisi
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<FinancialTransactionService>();

        // TCMB HTTP Client
        services.AddHttpClient("tcmb", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
            c.DefaultRequestHeaders.Add("User-Agent", "Oravity/1.0 (+https://oravity.com)");
        });

        // Kimlik doğrulama: OravityJwt (varsayılan) + PatientPortal JWT + OIDC (Microsoft, isteğe bağlı)
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret ayarı eksik. appsettings.json dosyasını kontrol edin.");

        var portalSecret = configuration["Jwt:PortalSecret"]
            ?? throw new InvalidOperationException("Jwt:PortalSecret ayarı eksik.");

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "OravityJwt";
            options.DefaultChallengeScheme    = "OravityJwt";
        });

        // OIDC correlation / geçiş için (SignInScheme); yanıt JSON ile döner
        authBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        authBuilder
            .AddJwtBearer("OravityJwt", options =>
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

        var msAuthority = configuration["Sso:Microsoft:Authority"];
        var msClientId  = configuration["Sso:Microsoft:ClientId"];
        if (!string.IsNullOrWhiteSpace(msAuthority) && !string.IsNullOrWhiteSpace(msClientId))
        {
            authBuilder.AddOpenIdConnect("Microsoft", options =>
            {
                options.Authority   = msAuthority.TrimEnd('/');
                options.ClientId    = msClientId;
                options.ClientSecret = configuration["Sso:Microsoft:ClientSecret"] ?? "";
                options.CallbackPath = "/api/auth/sso/callback/microsoft";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.SaveTokens   = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.GetClaimsFromUserInfoEndpoint = true;

                options.Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = ctx =>
                    {
                        ctx.Response.Redirect("/login?error=sso");
                        ctx.HandleResponse();
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async ctx =>
                    {
                        var handler = ctx.HttpContext.RequestServices.GetRequiredService<SsoCallbackHandler>();
                        var ip      = ctx.HttpContext.Connection.RemoteIpAddress?.ToString();
                        var result = await handler.HandleCallback(
                            "microsoft",
                            ctx.Principal!,
                            ip,
                            ctx.HttpContext.RequestAborted);
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                        ctx.Response.ContentType  = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsJsonAsync(
                            result,
                            ctx.HttpContext.RequestAborted);
                    }
                };
            });
        }

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

        // MinIO (S3 uyumlu) dosya depolama
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(configuration["Minio:Endpoint"] ?? "localhost:9000")
            .WithCredentials(
                configuration["Minio:AccessKey"] ?? "oravity",
                configuration["Minio:SecretKey"] ?? "oravity123")
            .WithSSL(false));

        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        return services;
    }
}
