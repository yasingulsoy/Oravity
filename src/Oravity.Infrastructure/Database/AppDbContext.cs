using Microsoft.EntityFrameworkCore;
using Oravity.SharedKernel.BaseEntities;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Infrastructure.Database;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // ─── Reference / Lookup ────────────────────────────────────────────────
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Vertical> Verticals => Set<Vertical>();

    // ─── Tenant Hierarchy ─────────────────────────────────────────────────
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();

    // ─── Users & Auth ──────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RoleTemplate> RoleTemplates => Set<RoleTemplate>();
    public DbSet<RoleTemplatePermission> RoleTemplatePermissions => Set<RoleTemplatePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

    // ─── Outbox ────────────────────────────────────────────────────────────
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // ─── Auth Tokens ───────────────────────────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureEntities(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
        SeedData(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (_currentUser?.IsAuthenticated == true)
                        entry.Entity.SetCreatedBy(_currentUser.UserId, _currentUser.TenantId);
                    break;
                case EntityState.Modified:
                    if (_currentUser?.IsAuthenticated == true)
                        entry.Entity.SetUpdatedBy(_currentUser.UserId);
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    // ─── Entity Configurations ────────────────────────────────────────────
    private static void ConfigureEntities(ModelBuilder m)
    {
        // ── Language ──────────────────────────────────────────────────────
        m.Entity<Language>(e =>
        {
            e.ToTable("languages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Code).HasMaxLength(5).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NativeName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Direction).HasMaxLength(3).HasDefaultValue("ltr");
            e.Property(x => x.FlagEmoji).HasMaxLength(10);
        });

        // ── Vertical ──────────────────────────────────────────────────────
        m.Entity<Vertical>(e =>
        {
            e.ToTable("verticals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.BodyChartType).HasMaxLength(50);
            e.Property(x => x.DefaultModules).HasColumnType("text[]");
            e.Property(x => x.ProviderLabel).HasMaxLength(100).HasDefaultValue("Hekim");
            e.Property(x => x.PatientLabel).HasMaxLength(100).HasDefaultValue("Hasta");
            e.Property(x => x.TreatmentLabel).HasMaxLength(100).HasDefaultValue("Tedavi");
        });

        // ── Company ───────────────────────────────────────────────────────
        m.Entity<Company>(e =>
        {
            e.ToTable("companies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultLanguageCode).HasMaxLength(5).HasDefaultValue("tr");
            e.HasOne(x => x.Vertical)
             .WithMany(v => v.Companies)
             .HasForeignKey(x => x.VerticalId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Branch ────────────────────────────────────────────────────────
        m.Entity<Branch>(e =>
        {
            e.ToTable("branches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultLanguageCode).HasMaxLength(5).HasDefaultValue("tr");
            e.HasOne(x => x.Company)
             .WithMany(c => c.Branches)
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vertical)
             .WithMany(v => v.Branches)
             .HasForeignKey(x => x.VerticalId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── User ──────────────────────────────────────────────────────────
        m.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Email).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.PreferredLanguageCode).HasMaxLength(5);
            e.Property(x => x.LastLoginAt);
        });

        // ── Permission ────────────────────────────────────────────────────
        m.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
        });

        // ── RoleTemplate ──────────────────────────────────────────────────
        m.Entity<RoleTemplate>(e =>
        {
            e.ToTable("role_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        // ── RoleTemplatePermission (junction) ─────────────────────────────
        m.Entity<RoleTemplatePermission>(e =>
        {
            e.ToTable("role_template_permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Ignore(x => x.PublicId);
            e.HasIndex(x => new { x.RoleTemplateId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.RoleTemplate)
             .WithMany(r => r.RoleTemplatePermissions)
             .HasForeignKey(x => x.RoleTemplateId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission)
             .WithMany(p => p.RoleTemplatePermissions)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserRoleAssignment ────────────────────────────────────────────
        m.Entity<UserRoleAssignment>(e =>
        {
            e.ToTable("user_role_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique();
            e.HasOne(x => x.User)
             .WithMany(u => u.RoleAssignments)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RoleTemplate)
             .WithMany(r => r.UserRoleAssignments)
             .HasForeignKey(x => x.RoleTemplateId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Company)
             .WithMany(c => c.UserRoleAssignments)
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch)
             .WithMany(b => b.UserRoleAssignments)
             .HasForeignKey(x => x.BranchId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UserPermissionOverride (junction) ─────────────────────────────
        m.Entity<UserPermissionOverride>(e =>
        {
            e.ToTable("user_permission_overrides");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Ignore(x => x.PublicId);
            e.HasOne(x => x.User)
             .WithMany(u => u.PermissionOverrides)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission)
             .WithMany(p => p.UserPermissionOverrides)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OutboxMessage ─────────────────────────────────────────────────
        m.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Ignore(x => x.PublicId);
            e.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            e.HasIndex(x => new { x.Status, x.NextRetryAt })
             .HasFilter("\"Status\" IN (1, 3)")
             .HasDatabaseName("ix_outbox_pending");
        });

        // ── RefreshToken ──────────────────────────────────────────────────
        m.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_hash");
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LoginAttempt ──────────────────────────────────────────────────
        m.Entity<LoginAttempt>(e =>
        {
            e.ToTable("login_attempts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Identifier).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.Identifier, x.CreatedAt })
             .HasDatabaseName("ix_login_attempts_identifier_created");
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.CreatedAt).IsRequired();
        });
    }

    // ─── Global Soft-Delete Filters ───────────────────────────────────────
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)) continue;

            // Reference/lookup tablolar ve junction tablolar: generic soft-delete filtresi uygulanmaz.
            // Bunlara aşağıda özel filtreler eklenir.
            var ignored = new[]
            {
                typeof(Language),
                typeof(Vertical),
                typeof(Permission),
                typeof(RoleTemplate),
                typeof(RoleTemplatePermission),
                typeof(UserPermissionOverride),
                typeof(UserRoleAssignment),
                typeof(OutboxMessage)
            };

            if (Array.Exists(ignored, t => t == entityType.ClrType)) continue;

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
        }

        // Junction tablolar için eşleşen filtreler:
        // User soft-delete'i junction navigasyonunu null bırakmaz.
        modelBuilder.Entity<UserRoleAssignment>()
            .HasQueryFilter(x => !x.IsDeleted && !x.User.IsDeleted);

        modelBuilder.Entity<UserPermissionOverride>()
            .HasQueryFilter(x => !x.IsDeleted && !x.User.IsDeleted);

        // RefreshToken: silinmiş kullanıcıya ait token'ları gizle
        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(x => !x.User.IsDeleted);
    }

    // ─── Seed Data ────────────────────────────────────────────────────────
    private static void SeedData(ModelBuilder m)
    {
        SeedLanguages(m);
        SeedVerticals(m);
        SeedRoleTemplates(m);
    }

    private static void SeedLanguages(ModelBuilder m)
    {
        m.Entity<Language>().HasData(
            new { Id = 1L, PublicId = new Guid("00000001-0000-0000-0000-000000000001"), Code = "tr", Name = "Türkçe",   NativeName = "Türkçe",    Direction = "ltr", FlagEmoji = "🇹🇷", IsActive = true, IsDefault = true,  SortOrder = 0, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 2L, PublicId = new Guid("00000001-0000-0000-0000-000000000002"), Code = "en", Name = "İngilizce", NativeName = "English",   Direction = "ltr", FlagEmoji = "🇬🇧", IsActive = true, IsDefault = false, SortOrder = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 3L, PublicId = new Guid("00000001-0000-0000-0000-000000000003"), Code = "ar", Name = "Arapça",   NativeName = "العربية",   Direction = "rtl", FlagEmoji = "🇸🇦", IsActive = true, IsDefault = false, SortOrder = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 4L, PublicId = new Guid("00000001-0000-0000-0000-000000000004"), Code = "ru", Name = "Rusça",    NativeName = "Русский",   Direction = "ltr", FlagEmoji = "🇷🇺", IsActive = true, IsDefault = false, SortOrder = 3, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 5L, PublicId = new Guid("00000001-0000-0000-0000-000000000005"), Code = "de", Name = "Almanca",  NativeName = "Deutsch",   Direction = "ltr", FlagEmoji = "🇩🇪", IsActive = true, IsDefault = false, SortOrder = 4, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false }
        );
    }

    private static void SeedVerticals(ModelBuilder m)
    {
        m.Entity<Vertical>().HasData(
            new { Id = 1L,  PublicId = new Guid("00000002-0000-0000-0000-000000000001"), Code = "DENTAL",     Name = "Diş Hekimliği",           HasBodyChart = true,  BodyChartType = "DENTAL_FDI",  DefaultModules = new[] { "CORE", "FINANCE", "APPOINTMENT", "TREATMENT" }, ProviderLabel = "Hekim",         PatientLabel = "Hasta",    TreatmentLabel = "Tedavi",    RequiresKts = true,  IsActive = true,  SortOrder = 0, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 2L,  PublicId = new Guid("00000002-0000-0000-0000-000000000002"), Code = "AESTHETIC",  Name = "Estetik & Güzellik",       HasBodyChart = false, BodyChartType = (string?)null, DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Uzman",         PatientLabel = "Müşteri",  TreatmentLabel = "Uygulama",  RequiresKts = false, IsActive = false, SortOrder = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 3L,  PublicId = new Guid("00000002-0000-0000-0000-000000000003"), Code = "NUTRITION",  Name = "Diyetisyen",               HasBodyChart = false, BodyChartType = (string?)null, DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Diyetisyen",    PatientLabel = "Danışan",  TreatmentLabel = "Seans",     RequiresKts = false, IsActive = false, SortOrder = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 4L,  PublicId = new Guid("00000002-0000-0000-0000-000000000004"), Code = "HAIR",       Name = "Saç Ekim",                 HasBodyChart = true,  BodyChartType = "HAIR_MAP",    DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Uzman",         PatientLabel = "Hasta",    TreatmentLabel = "Operasyon", RequiresKts = false, IsActive = false, SortOrder = 3, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 5L,  PublicId = new Guid("00000002-0000-0000-0000-000000000005"), Code = "PODOLOGY",   Name = "Ayak Bakımı",              HasBodyChart = true,  BodyChartType = "BODY_REGION", DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Podolog",       PatientLabel = "Hasta",    TreatmentLabel = "Uygulama",  RequiresKts = false, IsActive = false, SortOrder = 4, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 6L,  PublicId = new Guid("00000002-0000-0000-0000-000000000006"), Code = "PHYSIO",     Name = "Fizik Tedavi",             HasBodyChart = true,  BodyChartType = "BODY_REGION", DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Fizyoterapist", PatientLabel = "Hasta",    TreatmentLabel = "Seans",     RequiresKts = true,  IsActive = false, SortOrder = 5, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 7L,  PublicId = new Guid("00000002-0000-0000-0000-000000000007"), Code = "PSYCHOLOGY", Name = "Psikoloji",                HasBodyChart = false, BodyChartType = (string?)null, DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Terapist",      PatientLabel = "Danışan",  TreatmentLabel = "Seans",     RequiresKts = false, IsActive = false, SortOrder = 6, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 8L,  PublicId = new Guid("00000002-0000-0000-0000-000000000008"), Code = "VETERINARY", Name = "Veteriner",                HasBodyChart = false, BodyChartType = (string?)null, DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Veteriner",     PatientLabel = "Hasta",    TreatmentLabel = "Tedavi",    RequiresKts = false, IsActive = false, SortOrder = 7, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 9L,  PublicId = new Guid("00000002-0000-0000-0000-000000000009"), Code = "GENERAL",    Name = "Genel Muayenehane",        HasBodyChart = false, BodyChartType = (string?)null, DefaultModules = new[] { "CORE", "APPOINTMENT" },                          ProviderLabel = "Hekim",         PatientLabel = "Hasta",    TreatmentLabel = "Muayene",   RequiresKts = true,  IsActive = false, SortOrder = 8, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false }
        );
    }

    private static void SeedRoleTemplates(ModelBuilder m)
    {
        m.Entity<RoleTemplate>().HasData(
            new { Id = 1L, PublicId = new Guid("00000003-0000-0000-0000-000000000001"), Code = "BRANCH_MANAGER", Name = "Şube Yöneticisi",  Description = "Şube içindeki tüm işlemleri yönetir",              IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 2L, PublicId = new Guid("00000003-0000-0000-0000-000000000002"), Code = "DOCTOR",         Name = "Hekim",            Description = "Klinik muayene ve tedavi işlemlerini yürütür",     IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 3L, PublicId = new Guid("00000003-0000-0000-0000-000000000003"), Code = "ASSISTANT",      Name = "Asistan",          Description = "Hekime yardımcı klinik personel",                  IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 4L, PublicId = new Guid("00000003-0000-0000-0000-000000000004"), Code = "RECEPTIONIST",   Name = "Resepsiyonist",    Description = "Randevu ve hasta kayıt işlemlerini yönetir",       IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 5L, PublicId = new Guid("00000003-0000-0000-0000-000000000005"), Code = "ACCOUNTANT",     Name = "Muhasebeci",       Description = "Mali işlemler ve raporlama",                        IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false },
            new { Id = 6L, PublicId = new Guid("00000003-0000-0000-0000-000000000006"), Code = "READONLY",       Name = "Salt Okunur",      Description = "Yalnızca görüntüleme yetkisi",                     IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = (DateTime?)null, IsDeleted = false }
        );
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type type)
    {
        var param = System.Linq.Expressions.Expression.Parameter(type, "e");
        var prop  = System.Linq.Expressions.Expression.Property(param, nameof(BaseEntity.IsDeleted));
        var body  = System.Linq.Expressions.Expression.Equal(prop, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }
}
