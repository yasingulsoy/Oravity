using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Oravity.Infrastructure.Database;
using Oravity.IntegrationTests.Infrastructure;
using Oravity.SharedKernel.Entities;
using Xunit;

namespace Oravity.IntegrationTests;

/// <summary>
/// Auth modülü integration testleri.
/// Testcontainers PostgreSQL + Redis kullanır.
/// </summary>
[Collection("Integration")]
public class AuthIntegrationTests : IClassFixture<OravityWebAppFactory>, IAsyncLifetime
{
    private readonly OravityWebAppFactory _factory;
    private HttpClient _client = default!;

    private const string AdminEmail    = "admin@test.dev";
    private const string AdminPassword = "Test@12345!";

    public AuthIntegrationTests(OravityWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─── Setup / Teardown ────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _factory.InitializeDbAsync();
        _client = _factory.CreateClient();
        await SeedTestUserAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── Test 1 — Başarılı login → token al ─────────────────────────────
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = AdminEmail,
            password = AdminPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.ExpiresIn.Should().BeGreaterThan(0);
    }

    // ─── Test 2 — Token al → /api/auth/me endpoint test ─────────────────
    [Fact]
    public async Task Login_ThenCallMe_ShouldReturnClaims()
    {
        // Giriş yap
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = AdminEmail,
            password = AdminPassword
        });
        loginResp.EnsureSuccessStatusCode();
        var tokens = await loginResp.Content.ReadFromJsonAsync<LoginResponseDto>();

        // Token ile /me çağır
        using var meClient = _factory.CreateClient();
        meClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var meResp = await meClient.GetAsync("/api/auth/me");

        meResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await meResp.Content.ReadAsStringAsync();
        body.Should().Contain("email");
    }

    // ─── Test 3 — Hatalı şifre → 401 ────────────────────────────────────
    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = AdminEmail,
            password = "WrongPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Test 4 — 5 hatalı deneme → 429 ─────────────────────────────────
    [Fact]
    public async Task Login_After5FailedAttempts_ShouldReturn429()
    {
        // 5 hatalı deneme
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email    = AdminEmail,
                password = $"WrongPass{i}"
            });
        }

        // 6. deneme → 429 beklenir
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = AdminEmail,
            password = "AnotherWrongPass"
        });

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ─── Test 5 — Token olmadan /me → 401 ────────────────────────────────
    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    private async Task SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Daha önce eklendiyse atla
        if (db.Users.Any(u => u.Email == AdminEmail)) return;

        var hash = BCrypt.Net.BCrypt.HashPassword(AdminPassword);
        var user = User.Create(AdminEmail, "Test Admin", hash, isPlatformAdmin: false);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    // ─── Response DTO ─────────────────────────────────────────────────────
    private record LoginResponseDto(
        string AccessToken,
        string RefreshToken,
        int    ExpiresIn,
        string TokenType);
}
