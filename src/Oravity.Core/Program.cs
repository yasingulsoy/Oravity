using Microsoft.OpenApi.Models;
using Oravity.Core.Middleware;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Appointment.Infrastructure.Hubs;
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
            Description = "JWT token girin. Örnek: eyJhbGci..."
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

    // SignalR — real-time takvim
    builder.Services.AddSignalR();
    builder.Services.AddScoped<ICalendarBroadcastService, CalendarBroadcastService>();

    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

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
    app.MapHub<CalendarHub>("/hubs/calendar");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Oravity.Core terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
