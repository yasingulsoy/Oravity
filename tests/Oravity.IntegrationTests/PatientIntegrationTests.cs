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
/// Hasta CRUD + tenant izolasyonu integration testleri.
/// Platform admin JWT (role=1) tüm tenant'ları görür.
/// Branch-level JWT (role=3) sadece kendi şubesini görür.
/// </summary>
[Collection("Integration")]
public class PatientIntegrationTests : IClassFixture<OravityWebAppFactory>, IAsyncLifetime
{
    private readonly OravityWebAppFactory _factory;

    // ─── Test sabitleri ───────────────────────────────────────────────────
    private const long Company1 = 10;
    private const long Branch1  = 100;
    private const long Company2 = 20;
    private const long Branch2  = 200;
    private const long User1    = 1001;
    private const long User2    = 1002;

    public PatientIntegrationTests(OravityWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─── Setup ───────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _factory.InitializeDbAsync();
        await SeedTestTenantDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── Test 1 — Hasta oluştur (platform admin) ─────────────────────────
    [Fact]
    public async Task CreatePatient_AsAdmin_ShouldReturn201()
    {
        var client = _factory.CreateAdminClient(Company1, Branch1);

        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Mehmet",
            lastName  = "Yılmaz",
            phone     = "5551112233",
            email     = (string?)null,
            birthDate = (string?)null,
            gender    = "Erkek",
            tcNumber  = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PatientResponseDto>();
        body.Should().NotBeNull();
        body!.FirstName.Should().Be("Mehmet");
        body.PublicId.Should().NotBeEmpty();
    }

    // ─── Test 2 — Hasta getir ─────────────────────────────────────────────
    [Fact]
    public async Task CreateAndGetPatient_ShouldReturnSameData()
    {
        var client = _factory.CreateAdminClient(Company1, Branch1);

        // Oluştur
        var createResp = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Ayşe",
            lastName  = "Kaya",
            phone     = "5559998877",
            email     = "ayse@test.dev",
            gender    = "Kadın"
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponseDto>();

        // Getir
        var getResp = await client.GetAsync($"/api/patients/{created!.PublicId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResp.Content.ReadFromJsonAsync<PatientResponseDto>();
        fetched!.LastName.Should().Be("Kaya");
        fetched.Email.Should().Be("ayse@test.dev");
    }

    // ─── Test 3 — Hasta güncelle ──────────────────────────────────────────
    [Fact]
    public async Task UpdatePatient_ShouldReturnUpdatedData()
    {
        var client = _factory.CreateAdminClient(Company1, Branch1);

        var createResp = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Ali",
            lastName  = "Demir",
            phone     = "5550001122",
            gender    = "Erkek"
        });
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponseDto>();

        var updateResp = await client.PutAsJsonAsync($"/api/patients/{created!.PublicId}", new
        {
            firstName = "Ali",
            lastName  = "Yıldız",   // Soyad değişti
            phone     = "5550001122",
            gender    = "Erkek"
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResp.Content.ReadFromJsonAsync<PatientResponseDto>();
        updated!.LastName.Should().Be("Yıldız");
    }

    // ─── Test 4 — Hasta sil ───────────────────────────────────────────────
    [Fact]
    public async Task DeletePatient_ShouldReturn204_ThenNotFound()
    {
        var client = _factory.CreateAdminClient(Company1, Branch1);

        var createResp = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Silinecek",
            lastName  = "Hasta",
            phone     = "5556667788",
            gender    = "Erkek"
        });
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponseDto>();

        // Sil
        var deleteResp = await client.DeleteAsync($"/api/patients/{created!.PublicId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Tekrar istemeye çalış → 404
        var getResp = await client.GetAsync($"/api/patients/{created.PublicId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── Test 5 — Tenant izolasyonu: A tenant B'nin hastasını göremez ────
    [Fact]
    public async Task TenantIsolation_BranchUserCannotSeeOtherBranchPatients()
    {
        // Branch 1 admin hastayı oluşturur
        var adminClient = _factory.CreateAdminClient(Company1, Branch1);
        var createResp  = await adminClient.PostAsJsonAsync("/api/patients", new
        {
            firstName = "GizliHasta",
            lastName  = "Branch1",
            phone     = "5551234567",
            gender    = "Erkek"
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<PatientResponseDto>();

        // Branch 2 kullanıcısı kendi izinli client'i ile branch 1 hastasını GÖREMEMELI
        var branch2Client = _factory.CreateBranchClient(User2, Company2, Branch2);

        // Doğrudan ID ile erişim girişimi
        var getResp = await branch2Client.GetAsync($"/api/patients/{created!.PublicId}");

        // Branch 2 kullanıcısı ya 403 (izin yok — DB'de permission kaydı yok)
        // ya da 404 (tenant filtresi nedeniyle hasta görünmüyor) alır.
        getResp.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    // ─── Test 6 — Yetkisiz erişim: Token olmadan hasta listesi → 401 ─────
    [Fact]
    public async Task GetPatients_WithoutToken_ShouldReturn401()
    {
        var client = _factory.CreateClient();  // token yok

        var response = await client.GetAsync("/api/patients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Test 7 — Hasta arama ─────────────────────────────────────────────
    [Fact]
    public async Task SearchPatients_ByFirstName_ShouldReturnMatchingResults()
    {
        var client = _factory.CreateAdminClient(Company1, Branch1);

        // Seed
        await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Zeynep",
            lastName  = "Bulut",
            phone     = "5553334455",
            gender    = "Kadın"
        });

        var searchResp = await client.GetAsync("/api/patients?firstName=Zeynep");
        searchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await searchResp.Content.ReadFromJsonAsync<PagedResultDto>();
        body!.Items.Should().Contain(p => p.FirstName == "Zeynep");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    private async Task SeedTestTenantDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Idempotent: email ile kontrol et
        if (!db.Users.Any(u => u.Email == "user1@test.dev"))
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Test@12345!");
            db.Users.Add(User.Create("user1@test.dev", "User One", hash));
            db.Users.Add(User.Create("user2@test.dev", "User Two", hash));
            await db.SaveChangesAsync();
        }
    }

    // ─── Response DTO'lar ─────────────────────────────────────────────────
    private record PatientResponseDto(
        Guid    PublicId,
        string  FirstName,
        string  LastName,
        string? Phone,
        string? Email,
        string? Gender,
        bool    IsActive
    );

    private record PagedResultDto(
        List<PatientResponseDto> Items,
        int TotalCount,
        int Page,
        int PageSize
    );
}
