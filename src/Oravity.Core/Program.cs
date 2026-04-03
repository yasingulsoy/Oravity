using Hangfire;
using Microsoft.OpenApi.Models;
using Oravity.Core.Middleware;
using Oravity.Core.Modules.Core.Localization.Application.Services;
using Oravity.Core.Modules.Finance.EInvoice.Infrastructure.Adapters;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Appointment.Infrastructure.Hubs;
using Oravity.Core.Modules.Appointment.OnlineBooking.Application.Services;
using Oravity.Core.Modules.Core.DentalChart.Domain.Services;
using Oravity.Core.Modules.Core.PatientPortal.Infrastructure.Services;
using Oravity.Core.Modules.Survey.Application.Commands;
using Oravity.Core.Modules.Survey.Jobs;
using Oravity.Infrastructure.Jobs;
using Oravity.SharedKernel.Interfaces;
using Oravity.Core.Modules.Notification.Infrastructure.Hubs;
using Oravity.Core.Modules.Notification.Infrastructure.Services;
using Oravity.Infrastructure;
using Oravity.Infrastructure.Database;
using Oravity.Infrastructure.Tenancy;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Oravity.Core starting up...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341"));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Oravity Core API",
            Version = "v1",
            Description = "Process A — Hasta, Kullanıcı, Şube yönetimi"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Klinik personel JWT. Örnek: eyJhbGci..."
        });

        options.AddSecurityDefinition("PatientPortal", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Hasta portalı JWT (ayrı secret). Örnek: eyJhbGci..."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // SignalR — real-time takvim + bildirimler
    builder.Services.AddSignalR();
    builder.Services.AddScoped<ICalendarBroadcastService, CalendarBroadcastService>();
    builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

    // SMS Dispatch — Hangfire job için bağımlılıklar
    builder.Services.AddScoped<ISmsAdapter, StubSmsAdapter>();
    builder.Services.AddScoped<SmsDispatchService>();

    // FDI diş şeması servisi (stateless, singleton uygundur)
    builder.Services.AddSingleton<IFdiChartService, FdiChartService>();

    // Online randevu servisleri
    builder.Services.AddScoped<IOnlineAvailabilityService, OnlineAvailabilityService>();
    builder.Services.AddScoped<IOnlineBookingFilterService, OnlineBookingFilterService>();

    // Hasta portalı servisleri
    builder.Services.AddScoped<IPatientPortalJwtService, PatientPortalJwtService>();
    builder.Services.AddScoped<ICurrentPortalUser, CurrentPortalUserService>();

    // Survey & Complaint Hangfire jobs
    builder.Services.AddScoped<SurveySchedulerJob>();
    builder.Services.AddScoped<SlaMonitorJob>();
    builder.Services.AddScoped<ISendSurveyJob, SendSurveyJob>();

    // Outbox Processor
    builder.Services.AddScoped<OutboxProcessorJob>();
    builder.Services.AddScoped<OutboxEventDispatcher>();

    // Localization
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<TranslationService>();

    // E-Fatura adapters
    builder.Services.AddScoped<XmlExportAdapter>();
    builder.Services.AddScoped<ParasutAdapter>();
    builder.Services.AddScoped<LogoAdapter>();
    builder.Services.AddScoped<EInvoiceAdapterFactory>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<OutboxHealthCheck>(
            "outbox",
            tags: ["ready", "outbox"]);

    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    // Testing ortamında migration + seed factory tarafından ayrıca çalıştırılır
    if (!app.Environment.IsEnvironment("Testing"))
        await app.Services.SeedDatabaseAsync();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/api/health/live");
    app.MapHub<CalendarHub>("/hubs/calendar");
    app.MapHub<NotificationHub>("/hubs/notifications");

    // Hangfire recurring jobs — test ortamında schema henüz yok, atla
    if (!app.Environment.IsEnvironment("Testing"))
    {
        var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();

        jobManager.AddOrUpdate<SmsDispatchService>(
            "sms-dispatch",
            x => x.Execute(),
            Cron.Minutely());

        jobManager.AddOrUpdate<SurveySchedulerJob>(
            "survey-scheduler",
            x => x.Execute(),
            "*/5 * * * *");

        jobManager.AddOrUpdate<SlaMonitorJob>(
            "sla-monitor",
            x => x.Execute(),
            "*/30 * * * *");

        jobManager.AddOrUpdate<OutboxProcessorJob>(
            "outbox-processor",
            x => x.Execute(),
            "* * * * *");
    }

    app.Run();
}
// HostAbortedException: WebApplicationFactory (integration testler) tarafından
// kasıtlı olarak fırlatılır — yakalanmamalı, propagate olmalı.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"[STARTUP FATAL] {ex.GetType().Name}: {ex.Message}");
    Log.Fatal(ex, "Oravity.Core terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
