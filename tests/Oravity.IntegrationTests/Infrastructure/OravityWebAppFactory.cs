using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Oravity.Infrastructure.Database;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace Oravity.IntegrationTests.Infrastructure;

/// <summary>
/// Testcontainers tabanlı integration test ortamı.
/// PostgreSQL + Redis ephemeral container'lar ayağa kaldırır, migration uygular ve seed yapar.
/// </summary>
public class OravityWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("oravity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private bool _dbInitialized = false;

    public string PostgresConnectionString => _postgres.GetConnectionString();
    public string RedisConnectionString    => _redis.GetConnectionString();

    // ─── Container lifecycle ─────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }

    // ─── WebHost yapılandırması ──────────────────────────────────────────
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"]             = _redis.GetConnectionString(),
                ["Jwt:Secret"]          = TestConstants.JwtSecret,
                ["Jwt:PortalSecret"]    = TestConstants.JwtPortalSecret,
                ["Jwt:Issuer"]          = "Oravity.Core",
                ["Jwt:Audience"]        = "Oravity",
                ["Encryption:Key"]      = "T3JhdkRldktleTIwMjYhIVNlY3VyZUtleVRoaXMzMg=="
            });
        });

    }

    // ─── Migration + Seed ────────────────────────────────────────────────
    /// <summary>Migration ve seed işlemlerini yalnızca bir kez çalıştırır.</summary>
    public async Task InitializeDbAsync()
    {
        if (_dbInitialized) return;
        _dbInitialized = true;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    // ─── HTTP client yardımcıları ────────────────────────────────────────
    /// <summary>Platform admin (role=1) JWT ile önceden yapılandırılmış HTTP client.</summary>
    public HttpClient CreateAdminClient(long companyId = 1, long branchId = 1)
    {
        var token = JwtHelper.GenerateToken(
            userId: 1, roleLevel: 1, companyId: companyId, branchId: branchId);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Branch-level kullanıcı (role=3) JWT ile HTTP client.</summary>
    public HttpClient CreateBranchClient(long userId, long companyId, long branchId)
    {
        var token = JwtHelper.GenerateToken(
            userId: userId, roleLevel: 3, companyId: companyId, branchId: branchId);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

// ─── Sabitler ────────────────────────────────────────────────────────────

public static class TestConstants
{
    public const string JwtSecret       = "test-oravity-secret-key-min-32-characters-2026!";
    public const string JwtPortalSecret = "test-oravity-portal-secret-key-min-32-chars!";
}

// ─── JWT Token yardımcısı ─────────────────────────────────────────────────

public static class JwtHelper
{
    public static string GenerateToken(
        long userId,
        int  roleLevel,
        long companyId,
        long branchId,
        string email = "test@oravity.dev")
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestConstants.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub",         userId.ToString()),
            new Claim("email",       email),
            new Claim("company_id",  companyId.ToString()),
            new Claim("branch_id",   branchId.ToString()),
            new Claim("role_level",  roleLevel.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             "Oravity.Core",
            audience:           "Oravity",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
