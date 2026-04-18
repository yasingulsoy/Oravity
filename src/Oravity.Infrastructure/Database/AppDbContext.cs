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

    // ─── Patients ──────────────────────────────────────────────────────────
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientEmergencyContact> PatientEmergencyContacts => Set<PatientEmergencyContact>();
    public DbSet<CitizenshipType> CitizenshipTypes => Set<CitizenshipType>();
    public DbSet<ReferralSource> ReferralSources => Set<ReferralSource>();

    // ─── Appointments ──────────────────────────────────────────────────────
    public DbSet<Appointment>       Appointments       => Set<Appointment>();
    public DbSet<AppointmentStatus> AppointmentStatuses => Set<AppointmentStatus>();
    public DbSet<AppointmentType>   AppointmentTypes   => Set<AppointmentType>();
    public DbSet<Specialization>    Specializations    => Set<Specialization>();

    // ─── Hekim Takvimleri ─────────────────────────────────────────────────
    public DbSet<DoctorSchedule>       DoctorSchedules       => Set<DoctorSchedule>();
    public DbSet<DoctorSpecialDay>     DoctorSpecialDays     => Set<DoctorSpecialDay>();
    public DbSet<DoctorOnCallSettings> DoctorOnCallSettings  => Set<DoctorOnCallSettings>();

    // ─── Visit & Protocol ─────────────────────────────────────────────────
    public DbSet<Visit>               Visits               => Set<Visit>();
    public DbSet<Protocol>            Protocols            => Set<Protocol>();
    public DbSet<ProtocolSequence>    ProtocolSequences    => Set<ProtocolSequence>();
    public DbSet<ProtocolTypeSetting> ProtocolTypes        => Set<ProtocolTypeSetting>();

    // ─── ICD Kodu ─────────────────────────────────────────────────────────
    public DbSet<IcdCode> IcdCodes => Set<IcdCode>();

    // ─── Treatment Plans ───────────────────────────────────────────────────
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();

    // ─── Finance ───────────────────────────────────────────────────────────
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<AllocationApproval> AllocationApprovals => Set<AllocationApproval>();
    public DbSet<DoctorCommission> DoctorCommissions => Set<DoctorCommission>();
    public DbSet<DoctorCommissionTemplate> DoctorCommissionTemplates => Set<DoctorCommissionTemplate>();
    public DbSet<TemplateJobStartPrice> TemplateJobStartPrices => Set<TemplateJobStartPrice>();
    public DbSet<DoctorTemplateAssignment> DoctorTemplateAssignments => Set<DoctorTemplateAssignment>();
    public DbSet<DoctorTarget> DoctorTargets => Set<DoctorTarget>();
    public DbSet<BranchTarget> BranchTargets => Set<BranchTarget>();
    public DbSet<InstitutionInvoice> InstitutionInvoices => Set<InstitutionInvoice>();
    public DbSet<InstitutionPayment> InstitutionPayments => Set<InstitutionPayment>();

    // ─── Notifications ─────────────────────────────────────────────────────
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SmsQueue> SmsQueues => Set<SmsQueue>();

    // ─── Dental Chart ──────────────────────────────────────────────────────
    public DbSet<ToothRecord> ToothRecords => Set<ToothRecord>();
    public DbSet<ToothConditionHistory> ToothConditionHistories => Set<ToothConditionHistory>();

    // ─── Patient Records (Anamnesis, Medications, Notes, Files) ───────────
    public DbSet<PatientAnamnesis> PatientAnamneses => Set<PatientAnamnesis>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientNote> PatientNotes => Set<PatientNote>();
    public DbSet<PatientFile> PatientFiles => Set<PatientFile>();

    // ─── Online Booking ────────────────────────────────────────────────────
    public DbSet<DoctorOnlineBookingSettings> DoctorOnlineBookingSettings => Set<DoctorOnlineBookingSettings>();
    public DbSet<DoctorOnlineSchedule> DoctorOnlineSchedules => Set<DoctorOnlineSchedule>();
    public DbSet<DoctorOnlineBlock> DoctorOnlineBlocks => Set<DoctorOnlineBlock>();
    public DbSet<BranchOnlineBookingSettings> BranchOnlineBookingSettings => Set<BranchOnlineBookingSettings>();
    public DbSet<BranchCalendarSettings>      BranchCalendarSettings      => Set<BranchCalendarSettings>();
    public DbSet<OnlineBookingRequest> OnlineBookingRequests => Set<OnlineBookingRequest>();

    // ─── Patient Portal ────────────────────────────────────────────────────
    public DbSet<PatientPortalAccount> PatientPortalAccounts => Set<PatientPortalAccount>();
    public DbSet<PatientPortalSession> PatientPortalSessions => Set<PatientPortalSession>();

    // ─── Survey & Complaint ────────────────────────────────────────────────
    public DbSet<SurveyTemplate> SurveyTemplates => Set<SurveyTemplate>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintNote> ComplaintNotes => Set<ComplaintNote>();

    // ─── E-Fatura ─────────────────────────────────────────────────────────
    public DbSet<EInvoice> EInvoices => Set<EInvoice>();
    public DbSet<EInvoiceItem> EInvoiceItems => Set<EInvoiceItem>();
    public DbSet<EInvoiceIntegration> EInvoiceIntegrations => Set<EInvoiceIntegration>();

    // ─── Localization ──────────────────────────────────────────────────────
    public DbSet<TranslationKey> TranslationKeys => Set<TranslationKey>();
    public DbSet<Translation> Translations => Set<Translation>();

    // ─── Audit & KVKK ──────────────────────────────────────────────────────
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<KvkkConsentLog> KvkkConsentLogs => Set<KvkkConsentLog>();
    public DbSet<DataExportRequest> DataExportRequests => Set<DataExportRequest>();

    // ─── Döviz ────────────────────────────────────────────────────────────
    public DbSet<ExchangeRate>           ExchangeRates           => Set<ExchangeRate>();
    public DbSet<ExchangeRateOverride>   ExchangeRateOverrides   => Set<ExchangeRateOverride>();
    public DbSet<ExchangeRateDifference> ExchangeRateDifferences => Set<ExchangeRateDifference>();

    // ─── Tedavi Kataloğu ──────────────────────────────────────────────────
    public DbSet<Treatment>         Treatments         => Set<Treatment>();
    public DbSet<TreatmentCategory> TreatmentCategories => Set<TreatmentCategory>();

    // ─── Fiyatlandırma ────────────────────────────────────────────────────
    public DbSet<PricingRule>         PricingRules         => Set<PricingRule>();
    public DbSet<ReferencePriceList>  ReferencePriceLists  => Set<ReferencePriceList>();
    public DbSet<ReferencePriceItem>  ReferencePriceItems  => Set<ReferencePriceItem>();
    public DbSet<TreatmentMapping>    TreatmentMappings    => Set<TreatmentMapping>();
    public DbSet<Campaign>            Campaigns            => Set<Campaign>();

    // ─── Güvenlik (2FA / Cihaz / Politika) ───────────────────────────────
    public DbSet<User2FASettings>       User2FASettings        => Set<User2FASettings>();
    public DbSet<TrustedDevice>         TrustedDevices         => Set<TrustedDevice>();
    public DbSet<BranchSecurityPolicy>  BranchSecurityPolicies => Set<BranchSecurityPolicy>();

    // ─── Yedekleme ────────────────────────────────────────────────────────
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();

    // ─── Geo / Coğrafi Referans ───────────────────────────────────────────
    public DbSet<Country>     Countries    => Set<Country>();
    public DbSet<City>        Cities       => Set<City>();
    public DbSet<District>    Districts    => Set<District>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();

    // ─── Kurumlar ─────────────────────────────────────────────────────────
    public DbSet<Institution> Institutions => Set<Institution>();

    // ── Laboratuvar ──────────────────────────────────────────────────────
    public DbSet<Laboratory>                 Laboratories                 => Set<Laboratory>();
    public DbSet<LaboratoryBranchAssignment> LaboratoryBranchAssignments  => Set<LaboratoryBranchAssignment>();
    public DbSet<LaboratoryPriceItem>        LaboratoryPriceItems         => Set<LaboratoryPriceItem>();
    public DbSet<LaboratoryWork>             LaboratoryWorks              => Set<LaboratoryWork>();
    public DbSet<LaboratoryWorkItem>         LaboratoryWorkItems          => Set<LaboratoryWorkItem>();
    public DbSet<LaboratoryWorkHistory>      LaboratoryWorkHistories      => Set<LaboratoryWorkHistory>();
    public DbSet<LaboratoryApprovalAuthority> LaboratoryApprovalAuthorities => Set<LaboratoryApprovalAuthority>();

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
            e.Property(x => x.SsoProvider).HasMaxLength(50);
            e.Property(x => x.SsoSubject).HasMaxLength(200);
            e.Property(x => x.SsoEmail).HasMaxLength(200);
            e.HasIndex(x => new { x.SsoProvider, x.SsoSubject })
                .IsUnique()
                .HasDatabaseName("idx_users_sso")
                .HasFilter("\"SsoProvider\" IS NOT NULL");

            // ─── Hekim alanları
            e.Property(x => x.Title).HasMaxLength(50);
            e.Property(x => x.CalendarColor).HasMaxLength(7);
            e.Property(x => x.IsChiefPhysician).HasDefaultValue(false);
            e.HasOne(x => x.Specialization)
             .WithMany()
             .HasForeignKey(x => x.SpecializationId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
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

        // ── Patient ───────────────────────────────────────────────────────
        m.Entity<Patient>(e =>
        {
            e.ToTable("patients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_patients_public_id");

            // Kişisel
            e.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(200).IsRequired();
            e.Property(x => x.MotherName).HasMaxLength(200);
            e.Property(x => x.FatherName).HasMaxLength(200);
            e.Property(x => x.Gender).HasMaxLength(10);
            e.Property(x => x.MaritalStatus).HasMaxLength(20);
            e.Property(x => x.Nationality).HasMaxLength(100);
            e.Property(x => x.Occupation).HasMaxLength(200);
            e.Property(x => x.SmokingType).HasMaxLength(20);

            // Kimlik (şifreli)
            e.Property(x => x.TcNumberEncrypted).HasMaxLength(500);
            e.Property(x => x.TcNumberHash).HasMaxLength(64);
            e.HasIndex(x => x.TcNumberHash).HasDatabaseName("ix_patients_tc_hash");
            e.Property(x => x.PassportNoEncrypted).HasMaxLength(500);

            // İletişim
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => new { x.BranchId, x.Phone }).HasDatabaseName("ix_patients_branch_phone");
            e.Property(x => x.HomePhone).HasMaxLength(20);
            e.Property(x => x.WorkPhone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);

            // Adres
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.District).HasMaxLength(100);
            e.Property(x => x.Neighborhood).HasMaxLength(200);
            e.Property(x => x.Address).HasColumnType("text");

            // Ad + Soyad composite index (arama)
            e.HasIndex(x => new { x.BranchId, x.LastName, x.FirstName })
             .HasDatabaseName("ix_patients_branch_name");

            // Tıbbi / Geliş
            e.Property(x => x.BloodType).HasMaxLength(5);
            e.Property(x => x.ReferralPerson).HasMaxLength(200);
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.PreferredLanguageCode).HasMaxLength(5).HasDefaultValue("tr");
            e.Property(x => x.SmsOptIn).HasDefaultValue(true);
            e.Property(x => x.CampaignOptIn).HasDefaultValue(true);
            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CitizenshipType).WithMany().HasForeignKey(x => x.CitizenshipTypeId).IsRequired(false);
            e.HasOne(x => x.ReferralSource).WithMany().HasForeignKey(x => x.ReferralSourceId).IsRequired(false);
            e.HasOne(x => x.AgreementInstitution).WithMany().HasForeignKey(x => x.AgreementInstitutionId).IsRequired(false);
            e.HasOne(x => x.InsuranceInstitution).WithMany().HasForeignKey(x => x.InsuranceInstitutionId).IsRequired(false);
            e.HasMany(x => x.EmergencyContacts).WithOne(x => x.Patient).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);

            // Audit fields
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.CreatedByUserId);
            e.Property(x => x.UpdatedByUserId);
        });

        // ── PatientEmergencyContact ────────────────────────────────────────
        m.Entity<PatientEmergencyContact>(e =>
        {
            e.ToTable("patient_emergency_contacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Relationship).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasColumnType("text");
            e.HasIndex(x => new { x.PatientId, x.SortOrder }).IsUnique().HasDatabaseName("ix_patient_emergency_sort");
        });

        // ── CitizenshipType ────────────────────────────────────────────────
        m.Entity<CitizenshipType>(e =>
        {
            e.ToTable("citizenship_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            // Code unique per company (null = global; şirket kendi kodunu üstüne yazabilir)
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_citizenship_types_company_code");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // ── ReferralSource ─────────────────────────────────────────────────
        m.Entity<ReferralSource>(e =>
        {
            e.ToTable("referral_sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            // Code unique per company (null = global)
            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("ix_referral_sources_company_code");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Country ───────────────────────────────────────────────────────
        m.Entity<Country>(e =>
        {
            e.ToTable("countries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsoCode).HasMaxLength(3).IsRequired();
            e.HasIndex(x => x.IsoCode).IsUnique().HasDatabaseName("ix_countries_iso_code");
        });

        // ── City ──────────────────────────────────────────────────────────
        m.Entity<City>(e =>
        {
            e.ToTable("cities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Country).WithMany(x => x.Cities).HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── District ──────────────────────────────────────────────────────
        m.Entity<District>(e =>
        {
            e.ToTable("districts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.City).WithMany(x => x.Districts).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Nationality ───────────────────────────────────────────────────
        m.Entity<Nationality>(e =>
        {
            e.ToTable("nationalities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_nationalities_code");
        });

        // ── Institution ───────────────────────────────────────────────────
        m.Entity<Institution>(e =>
        {
            e.ToTable("institutions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => x.Code).HasDatabaseName("ix_institutions_code");
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.MarketSegment).HasMaxLength(20);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Website).HasMaxLength(300);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.District).HasMaxLength(100);
            e.Property(x => x.Address).HasColumnType("text");
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(30);
            e.Property(x => x.TaxNumber).HasMaxLength(20);
            e.Property(x => x.TaxOffice).HasMaxLength(200);
            e.Property(x => x.DiscountRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.PaymentDays).HasDefaultValue(30);
            e.Property(x => x.PaymentTerms).HasColumnType("text");
            e.Property(x => x.Notes).HasColumnType("text");
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Specialization ────────────────────────────────────────────────
        m.Entity<Specialization>(e =>
        {
            e.ToTable("specializations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_specializations_code");
        });

        // ── AppointmentStatus ─────────────────────────────────────────────
        m.Entity<AppointmentStatus>(e =>
        {
            e.ToTable("appointment_statuses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_appointment_statuses_code");
            e.Property(x => x.TitleColor).HasMaxLength(7).HasDefaultValue("#3598DC");
            e.Property(x => x.ContainerColor).HasMaxLength(7).HasDefaultValue("#4c4cff");
            e.Property(x => x.BorderColor).HasMaxLength(7).HasDefaultValue("#3333ff");
            e.Property(x => x.TextColor).HasMaxLength(7).HasDefaultValue("#ffffff");
            e.Property(x => x.ClassName).HasMaxLength(50).HasDefaultValue("cl-white");
            e.Property(x => x.AllowedNextStatusIds).HasColumnType("text").HasDefaultValue("[]");
        });

        // ── ProtocolType ──────────────────────────────────────────────────
        m.Entity<ProtocolTypeSetting>(e =>
        {
            e.ToTable("protocol_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever(); // ID'ler sabit (1-5)
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Color).HasMaxLength(7).HasDefaultValue("#6366f1");
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_protocol_types_code");
        });

        // ── AppointmentType ───────────────────────────────────────────────
        m.Entity<AppointmentType>(e =>
        {
            e.ToTable("appointment_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_appointment_types_code");
            e.Property(x => x.Color).HasMaxLength(7).HasDefaultValue("#3598DC");
        });

        // ── Appointment ───────────────────────────────────────────────────
        m.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_appointments_public_id");

            e.Property(x => x.StatusId).IsRequired();
            e.Property(x => x.StartTime).IsRequired();
            e.Property(x => x.EndTime).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.AppointmentNo).HasMaxLength(50);
            e.Property(x => x.BookingSource).HasMaxLength(50).HasDefaultValue("manual");
            e.Property(x => x.RowVersion).HasDefaultValue(1).IsConcurrencyToken();

            // Slot çakışması application katmanında (CreateAppointmentCommand) yönetilir.
            // Unique index kaldırıldı: appointment.create_overlap izinli kullanıcıların
            // aynı saat dilimine birden fazla randevu ekleyebilmesi için.

            e.HasIndex(x => new { x.BranchId, x.StartTime })
             .HasDatabaseName("ix_appointments_branch_start");
            e.HasIndex(x => new { x.DoctorId, x.StartTime })
             .HasDatabaseName("ix_appointments_doctor_start");

            e.Property(x => x.TenantId).IsRequired();

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Status)
             .WithMany()
             .HasForeignKey(x => x.StatusId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AppointmentType)
             .WithMany()
             .HasForeignKey(x => x.AppointmentTypeId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Specialization)
             .WithMany()
             .HasForeignKey(x => x.SpecializationId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorSchedule ────────────────────────────────────────────────
        m.Entity<DoctorSchedule>(e =>
        {
            e.ToTable("doctor_schedules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.DayOfWeek })
             .IsUnique()
             .HasDatabaseName("ix_doctor_schedules_unique");
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── DoctorSpecialDay ──────────────────────────────────────────────
        m.Entity<DoctorSpecialDay>(e =>
        {
            e.ToTable("doctor_special_days");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Reason).HasMaxLength(200);
            e.Property(x => x.Type).HasConversion<int>();
            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.SpecificDate })
             .IsUnique()
             .HasDatabaseName("ix_doctor_special_days_unique");
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── DoctorOnCallSettings ──────────────────────────────────────────
        m.Entity<DoctorOnCallSettings>(e =>
        {
            e.ToTable("doctor_on_call_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PeriodType).HasConversion<int>();
            e.HasIndex(x => new { x.DoctorId, x.BranchId })
             .IsUnique()
             .HasDatabaseName("ix_doctor_on_call_settings_unique");
            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── TreatmentPlan ──────────────────────────────────────────────────
        m.Entity<TreatmentPlan>(e =>
        {
            e.ToTable("treatment_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_treatment_plans_public_id");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_treatment_plans_patient");
            e.HasIndex(x => x.DoctorId).HasDatabaseName("ix_treatment_plans_doctor");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_treatment_plans_status");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Items)
             .WithOne(x => x.Plan)
             .HasForeignKey(x => x.PlanId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Protocol)
             .WithMany(x => x.TreatmentPlans)
             .HasForeignKey(x => x.ProtocolId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TreatmentPlanItem ──────────────────────────────────────────────
        m.Entity<TreatmentPlanItem>(e =>
        {
            e.ToTable("treatment_plan_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_treatment_plan_items_public_id");

            e.Property(x => x.TreatmentId).IsRequired();
            e.Property(x => x.ToothNumber).HasMaxLength(10);
            e.Property(x => x.ToothSurfaces).HasMaxLength(20);
            e.Property(x => x.BodyRegionCode).HasMaxLength(50);
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.DiscountRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.FinalPrice).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => x.PlanId).HasDatabaseName("ix_treatment_plan_items_plan");
            e.HasIndex(x => x.TreatmentId).HasDatabaseName("ix_treatment_plan_items_treatment");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_treatment_plan_items_status");

            e.HasOne(x => x.Treatment)
             .WithMany()
             .HasForeignKey(x => x.TreatmentId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payment ────────────────────────────────────────────────────────
        m.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_payments_public_id");

            e.Property(x => x.Amount).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.Method).IsRequired();
            e.Property(x => x.PaymentDate).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_payments_patient");
            e.HasIndex(x => new { x.BranchId, x.PaymentDate }).HasDatabaseName("ix_payments_branch_date");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Allocations)
             .WithOne(x => x.Payment)
             .HasForeignKey(x => x.PaymentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PaymentAllocation ──────────────────────────────────────────────
        m.Entity<PaymentAllocation>(e =>
        {
            e.ToTable("payment_allocations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.AllocatedAmount).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.Source).HasDefaultValue(AllocationSource.Patient);
            e.Property(x => x.Method).HasDefaultValue(AllocationMethod.Automatic);
            e.Property(x => x.BranchId).HasDefaultValue(0L);
            e.Property(x => x.AllocatedByUserId).HasDefaultValue(0L);
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => x.PaymentId).HasDatabaseName("ix_payment_alloc_payment");
            e.HasIndex(x => x.InstitutionPaymentId).HasDatabaseName("ix_payment_alloc_inst_payment");
            e.HasIndex(x => x.TreatmentPlanItemId).HasDatabaseName("ix_payment_alloc_item");
            e.HasIndex(x => new { x.BranchId, x.CreatedAt }).HasDatabaseName("ix_payment_alloc_branch_created");

            e.HasOne(x => x.TreatmentPlanItem)
             .WithMany()
             .HasForeignKey(x => x.TreatmentPlanItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.InstitutionPayment)
             .WithMany()
             .HasForeignKey(x => x.InstitutionPaymentId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);

            e.HasOne(x => x.Approval)
             .WithMany()
             .HasForeignKey(x => x.ApprovalId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── AllocationApproval ─────────────────────────────────────────────
        m.Entity<AllocationApproval>(e =>
        {
            e.ToTable("allocation_approvals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_allocation_approvals_public_id");

            e.Property(x => x.RequestedAmount).HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.Source).IsRequired();
            e.Property(x => x.RequestNotes).HasColumnType("text");
            e.Property(x => x.ApprovalNotes).HasColumnType("text");
            e.Property(x => x.RejectionReason).HasColumnType("text");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAt })
             .HasDatabaseName("ix_allocation_approvals_branch_status");
            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_allocation_approvals_patient");
            e.HasIndex(x => x.TreatmentPlanItemId).HasDatabaseName("ix_allocation_approvals_item");

            e.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TreatmentPlanItem).WithMany().HasForeignKey(x => x.TreatmentPlanItemId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorCommission ───────────────────────────────────────────────
        m.Entity<DoctorCommission>(e =>
        {
            e.ToTable("doctor_commissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.GrossAmount).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.CommissionRate).HasColumnType("numeric(5,2)").IsRequired();
            e.Property(x => x.CommissionAmount).HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.Status).IsRequired();

            e.Property(x => x.PosCommissionRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.PosCommissionAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.LabCostDeducted).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.TreatmentCostDeducted).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.TreatmentPlanCommissionDeducted).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.ExtraExpenseRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.ExtraExpenseAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.NetBaseAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.FixedFee).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.KdvRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.KdvAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.WithholdingTaxRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.WithholdingTaxAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.NetCommissionAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.BonusApplied).HasDefaultValue(false);
            e.Property(x => x.PeriodYear).HasDefaultValue(0);
            e.Property(x => x.PeriodMonth).HasDefaultValue(0);

            e.HasIndex(x => x.DoctorId).HasDatabaseName("ix_doctor_commission_doctor");
            e.HasIndex(x => x.TreatmentPlanItemId).HasDatabaseName("ix_doctor_commission_item");
            e.HasIndex(x => new { x.BranchId, x.Status }).HasDatabaseName("ix_doctor_commission_branch_status");
            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.PeriodYear, x.PeriodMonth })
             .HasDatabaseName("ix_doctor_commission_period");

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TreatmentPlanItem)
             .WithMany()
             .HasForeignKey(x => x.TreatmentPlanItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorCommissionTemplate ──────────────────────────────────────
        m.Entity<DoctorCommissionTemplate>(e =>
        {
            e.ToTable("doctor_commission_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_commission_templates_public_id");

            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.FixedFee).HasColumnType("numeric(14,2)").HasDefaultValue(0m);
            e.Property(x => x.PrimRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.ClinicTargetBonusRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.DoctorTargetBonusRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.KdvRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.KdvAppliedPaymentTypes).HasMaxLength(200);
            e.Property(x => x.ExtraExpenseRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.WithholdingTaxRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique()
             .HasDatabaseName("ix_commission_templates_company_name");

            e.HasMany(x => x.JobStartPrices)
             .WithOne(x => x.Template)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TemplateJobStartPrice ─────────────────────────────────────────
        m.Entity<TemplateJobStartPrice>(e =>
        {
            e.ToTable("commission_template_job_start_prices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Value).HasColumnType("numeric(14,2)").IsRequired();
            e.HasIndex(x => new { x.TemplateId, x.TreatmentId }).IsUnique()
             .HasDatabaseName("ix_job_start_prices_template_treatment");

            e.HasOne(x => x.Treatment)
             .WithMany()
             .HasForeignKey(x => x.TreatmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorTemplateAssignment ──────────────────────────────────────
        m.Entity<DoctorTemplateAssignment>(e =>
        {
            e.ToTable("doctor_template_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_doctor_template_assign_public_id");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.DoctorId, x.IsActive })
             .HasDatabaseName("ix_doctor_template_assign_doctor_active");

            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorTarget ──────────────────────────────────────────────────
        m.Entity<DoctorTarget>(e =>
        {
            e.ToTable("doctor_targets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.TargetAmount).HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.Year, x.Month }).IsUnique()
             .HasDatabaseName("ix_doctor_targets_doctor_branch_ym");

            e.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── BranchTarget ──────────────────────────────────────────────────
        m.Entity<BranchTarget>(e =>
        {
            e.ToTable("branch_targets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.TargetAmount).HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.BranchId, x.Year, x.Month }).IsUnique()
             .HasDatabaseName("ix_branch_targets_branch_ym");

            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── InstitutionInvoice ────────────────────────────────────────────
        m.Entity<InstitutionInvoice>(e =>
        {
            e.ToTable("institution_invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_institution_invoices_public_id");

            e.Property(x => x.InvoiceNo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.PaidAmount).HasColumnType("numeric(14,2)").HasDefaultValue(0m);
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.PaymentReferenceNo).HasMaxLength(100);
            e.Property(x => x.TreatmentItemIdsJson).HasColumnType("text");
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => new { x.BranchId, x.Status, x.DueDate })
             .HasDatabaseName("ix_institution_invoices_branch_status_due");
            e.HasIndex(x => x.InstitutionId).HasDatabaseName("ix_institution_invoices_institution");
            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_institution_invoices_patient");
            e.HasIndex(x => new { x.BranchId, x.InvoiceNo }).IsUnique()
             .HasDatabaseName("ix_institution_invoices_branch_no");

            e.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Payments)
             .WithOne(x => x.Invoice)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── InstitutionPayment ────────────────────────────────────────────
        m.Entity<InstitutionPayment>(e =>
        {
            e.ToTable("institution_payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_institution_payments_public_id");

            e.Property(x => x.Amount).HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.ReferenceNo).HasMaxLength(100);
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.TenantId).IsRequired();

            e.HasIndex(x => x.InvoiceId).HasDatabaseName("ix_institution_payments_invoice");
        });

        // ── Notification ───────────────────────────────────────────────────
        m.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_notifications_public_id");

            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasColumnType("text").IsRequired();
            e.Property(x => x.RelatedEntityType).HasMaxLength(50);

            // Okunmamış + kullanıcıya / role göre listeleme index'leri
            e.HasIndex(x => new { x.ToUserId, x.IsRead, x.CreatedAt })
             .HasDatabaseName("ix_notifications_to_user_read");
            e.HasIndex(x => new { x.BranchId, x.ToRole, x.IsRead, x.CreatedAt })
             .HasDatabaseName("ix_notifications_branch_role_read");

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ToUser)
             .WithMany()
             .HasForeignKey(x => x.ToUserId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SmsQueue ───────────────────────────────────────────────────────
        m.Entity<SmsQueue>(e =>
        {
            e.ToTable("sms_queue");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ToPhone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Message).HasColumnType("text").IsRequired();
            e.Property(x => x.SourceType).HasMaxLength(50);
            e.Property(x => x.ErrorMessage).HasColumnType("text");
            e.Property(x => x.ProviderMessageId).HasMaxLength(200);

            e.HasIndex(x => new { x.Status, x.NextRetryAt })
             .HasDatabaseName("ix_sms_queue_pending")
             .HasFilter("\"Status\" = 1");
            e.HasIndex(x => x.CompanyId).HasDatabaseName("ix_sms_queue_company");

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ToothRecord ────────────────────────────────────────────────────
        m.Entity<ToothRecord>(e =>
        {
            e.ToTable("tooth_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_tooth_records_public_id");

            // Bir hasta için aynı diş numarası benzersiz olmalı (partial: is_deleted = false)
            e.HasIndex(x => new { x.PatientId, x.ToothNumber })
             .IsUnique()
             .HasFilter("\"IsDeleted\" = false")
             .HasDatabaseName("ix_tooth_records_patient_tooth_unique");

            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_tooth_records_patient");

            e.Property(x => x.ToothNumber).HasMaxLength(5).IsRequired();
            e.Property(x => x.Surfaces).HasMaxLength(20);
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Recorder)
             .WithMany()
             .HasForeignKey(x => x.RecordedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ToothConditionHistory ──────────────────────────────────────────
        m.Entity<ToothConditionHistory>(e =>
        {
            e.ToTable("tooth_condition_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ToothNumber).HasMaxLength(5).IsRequired();
            e.Property(x => x.Reason).HasColumnType("text");

            e.HasIndex(x => new { x.PatientId, x.ToothNumber, x.ChangedAt })
             .HasDatabaseName("ix_tooth_history_patient_tooth");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Changer)
             .WithMany()
             .HasForeignKey(x => x.ChangedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientAnamnesis ───────────────────────────────────────────────
        m.Entity<PatientAnamnesis>(e =>
        {
            e.ToTable("patient_anamnesis");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_patient_anamnesis_public_id");

            // Hasta başına tek kayıt constraint kaldırıldı — her protokolde yeni anamnez yazılabilir
            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_patient_anamnesis_patient");

            e.HasOne(x => x.Protocol)
             .WithMany(x => x.Anamneses)
             .HasForeignKey(x => x.ProtocolId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.Property(x => x.BloodType).HasMaxLength(5);
            e.Property(x => x.AnticoagulantDrug).HasMaxLength(200);
            e.Property(x => x.SmokingAmount).HasMaxLength(50);
            e.Property(x => x.OtherSystemicDiseases).HasColumnType("text");
            e.Property(x => x.LocalAnesthesiaAllergyNote).HasColumnType("text");
            e.Property(x => x.OtherAllergies).HasColumnType("text");
            e.Property(x => x.PreviousSurgeries).HasColumnType("text");
            e.Property(x => x.AdditionalNotes).HasColumnType("text");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.FilledByUser)
             .WithMany()
             .HasForeignKey(x => x.FilledBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UpdatedByUser)
             .WithMany()
             .HasForeignKey(x => x.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Visit ─────────────────────────────────────────────────────────
        m.Entity<Visit>(e =>
        {
            e.ToTable("visits");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_visits_public_id");
            e.HasIndex(x => new { x.BranchId, x.VisitDate }).HasDatabaseName("ix_visits_branch_date");
            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_visits_patient");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Appointment)
             .WithMany()
             .HasForeignKey(x => x.AppointmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(x => x.Protocols)
             .WithOne(x => x.Visit)
             .HasForeignKey(x => x.VisitId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Protocol ──────────────────────────────────────────────────────
        m.Entity<Protocol>(e =>
        {
            e.ToTable("protocols");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_protocols_public_id");
            e.HasIndex(x => x.VisitId).HasDatabaseName("ix_protocols_visit");
            e.HasIndex(x => new { x.DoctorId, x.Status }).HasDatabaseName("ix_protocols_doctor_status");
            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_protocols_patient");
            e.HasIndex(x => new { x.BranchId, x.ProtocolYear }).HasDatabaseName("ix_protocols_branch_year");
            e.HasIndex(x => new { x.BranchId, x.ProtocolYear, x.ProtocolSeq })
             .IsUnique().HasDatabaseName("ix_protocols_no_unique");

            e.Property(x => x.ProtocolNo).HasMaxLength(20).IsRequired();
            e.Property(x => x.ProtocolType).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.ChiefComplaint).HasColumnType("text");
            e.Property(x => x.ExaminationFindings).HasColumnType("text");
            e.Property(x => x.Diagnosis).HasColumnType("text");
            e.Property(x => x.TreatmentPlan).HasColumnType("text");
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.IcdDiagnosesJson).HasColumnType("text").HasDefaultValue("[]");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ProtocolSequence ──────────────────────────────────────────────
        m.Entity<ProtocolSequence>(e =>
        {
            e.ToTable("protocol_sequences");
            e.HasKey(x => new { x.BranchId, x.Year });
            e.Property(x => x.LastSeq).HasDefaultValue(0);

            e.HasOne<Branch>()
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── IcdCode ───────────────────────────────────────────────────────
        m.Entity<IcdCode>(e =>
        {
            e.ToTable("icd_codes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_icd_codes_public_id");
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_icd_codes_code");
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Category).HasMaxLength(20).IsRequired();
        });


        // ── PatientMedication ──────────────────────────────────────────────
        m.Entity<PatientMedication>(e =>
        {
            e.ToTable("patient_medications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.DrugName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Dose).HasMaxLength(100);
            e.Property(x => x.Frequency).HasMaxLength(100);
            e.Property(x => x.Reason).HasMaxLength(300);

            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_patient_medications_patient");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AddedByUser)
             .WithMany()
             .HasForeignKey(x => x.AddedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientNote ────────────────────────────────────────────────────
        m.Entity<PatientNote>(e =>
        {
            e.ToTable("patient_notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_patient_notes_public_id");

            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Content).HasColumnType("text").IsRequired();

            // Pinlenmiş önce, sonra oluşturma tarihi azalan
            e.HasIndex(x => new { x.PatientId, x.IsPinned, x.CreatedAt })
             .HasDatabaseName("ix_patient_notes_patient_pinned");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CreatedByUser)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UpdatedByUser)
             .WithMany()
             .HasForeignKey(x => x.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientFile ────────────────────────────────────────────────────
        m.Entity<PatientFile>(e =>
        {
            e.ToTable("patient_files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_patient_files_public_id");

            e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.FileExt).HasMaxLength(10);
            e.Property(x => x.Note).HasColumnType("text");

            e.HasIndex(x => new { x.PatientId, x.FileType })
             .HasDatabaseName("ix_patient_files_patient_type");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UploadedByUser)
             .WithMany()
             .HasForeignKey(x => x.UploadedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorOnlineBookingSettings ────────────────────────────────────
        m.Entity<DoctorOnlineBookingSettings>(e =>
        {
            e.ToTable("doctor_online_booking_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.BookingNote).HasColumnType("text");

            e.HasIndex(x => new { x.DoctorId, x.BranchId }).IsUnique()
             .HasDatabaseName("ix_doctor_online_settings_unique");

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorOnlineSchedule ───────────────────────────────────────────
        m.Entity<DoctorOnlineSchedule>(e =>
        {
            e.ToTable("doctor_online_schedule");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.DayOfWeek }).IsUnique()
             .HasDatabaseName("ix_doctor_online_schedule_unique");

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DoctorOnlineBlock ──────────────────────────────────────────────
        m.Entity<DoctorOnlineBlock>(e =>
        {
            e.ToTable("doctor_online_blocks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Reason).HasMaxLength(200);

            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.StartDatetime })
             .HasDatabaseName("ix_doctor_online_blocks_doctor_date");

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Creator)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BranchOnlineBookingSettings ────────────────────────────────────
        m.Entity<BranchOnlineBookingSettings>(e =>
        {
            e.ToTable("branch_online_booking_settings");
            e.HasKey(x => x.BranchId);

            e.Property(x => x.WidgetSlug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.WidgetSlug).IsUnique()
             .HasDatabaseName("ix_branch_online_settings_slug");

            e.Property(x => x.PrimaryColor).HasMaxLength(7).HasDefaultValue("#2563eb");
            e.Property(x => x.LogoUrl).HasMaxLength(500);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BranchCalendarSettings ────────────────────────────────────────
        m.Entity<BranchCalendarSettings>(e =>
        {
            e.ToTable("branch_calendar_settings");
            e.HasKey(x => x.BranchId);

            e.Property(x => x.SlotIntervalMinutes).HasDefaultValue(30);
            e.Property(x => x.DayStartHour).HasDefaultValue(8);
            e.Property(x => x.DayEndHour).HasDefaultValue(20);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OnlineBookingRequest ───────────────────────────────────────────
        m.Entity<OnlineBookingRequest>(e =>
        {
            e.ToTable("online_booking_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_online_booking_requests_public_id");

            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.VerificationCode).HasMaxLength(6);
            e.Property(x => x.PatientNote).HasColumnType("text");
            e.Property(x => x.RejectionReason).HasColumnType("text");

            e.HasIndex(x => new { x.BranchId, x.Status })
             .HasDatabaseName("ix_online_booking_requests_branch_status");

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Reviewer)
             .WithMany()
             .HasForeignKey(x => x.ReviewedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientPortalAccount ───────────────────────────────────────────
        m.Entity<PatientPortalAccount>(e =>
        {
            e.ToTable("patient_portal_accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_patient_portal_accounts_public_id");

            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique()
             .HasDatabaseName("ix_patient_portal_accounts_email");

            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.EmailVerificationToken).HasMaxLength(200);
            e.Property(x => x.PhoneVerificationCode).HasMaxLength(6);
            e.Property(x => x.PreferredLanguageCode).HasMaxLength(5).HasDefaultValue("tr");

            // patient_id UNIQUE — her hastanın tek portal hesabı olabilir
            e.HasIndex(x => x.PatientId).IsUnique()
             .HasDatabaseName("ix_patient_portal_accounts_patient")
             .HasFilter("\"PatientId\" IS NOT NULL");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PatientPortalSession ───────────────────────────────────────────
        m.Entity<PatientPortalSession>(e =>
        {
            e.ToTable("patient_portal_sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(500);

            e.HasIndex(x => x.TokenHash)
             .HasDatabaseName("ix_patient_portal_sessions_token");

            e.HasIndex(x => new { x.AccountId, x.IsRevoked })
             .HasDatabaseName("ix_patient_portal_sessions_account_active");

            e.HasOne(x => x.Account)
             .WithMany()
             .HasForeignKey(x => x.AccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SurveyTemplate ─────────────────────────────────────────────────
        m.Entity<SurveyTemplate>(e =>
        {
            e.ToTable("survey_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_survey_templates_public_id");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");

            e.HasIndex(x => new { x.CompanyId, x.IsActive })
             .HasDatabaseName("ix_survey_templates_company_active");

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Creator)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SurveyQuestion ────────────────────────────────────────────────
        m.Entity<SurveyQuestion>(e =>
        {
            e.ToTable("survey_questions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.QuestionText).HasColumnType("text").IsRequired();
            e.Property(x => x.Options).HasColumnType("jsonb");

            e.HasIndex(x => new { x.TemplateId, x.SortOrder })
             .HasDatabaseName("ix_survey_questions_template_sort");

            e.HasOne(x => x.Template)
             .WithMany(t => t.Questions)
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SurveyResponse ────────────────────────────────────────────────
        m.Entity<SurveyResponse>(e =>
        {
            e.ToTable("survey_responses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_survey_responses_public_id");

            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique()
             .HasDatabaseName("ix_survey_responses_token");

            e.Property(x => x.AverageScore).HasColumnType("numeric(3,1)");

            e.HasIndex(x => new { x.PatientId, x.TemplateId })
             .HasDatabaseName("ix_survey_responses_patient_template");

            e.HasOne(x => x.Template)
             .WithMany()
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Appointment)
             .WithMany()
             .HasForeignKey(x => x.AppointmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SurveyAnswer ──────────────────────────────────────────────────
        m.Entity<SurveyAnswer>(e =>
        {
            e.ToTable("survey_answers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.AnswerText).HasColumnType("text");
            e.Property(x => x.SelectedOption).HasMaxLength(200);

            e.HasOne(x => x.Response)
             .WithMany(r => r.Answers)
             .HasForeignKey(x => x.ResponseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Question)
             .WithMany()
             .HasForeignKey(x => x.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Complaint ─────────────────────────────────────────────────────
        m.Entity<Complaint>(e =>
        {
            e.ToTable("complaints");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_complaints_public_id");

            e.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasColumnType("text").IsRequired();
            e.Property(x => x.Resolution).HasColumnType("text");

            e.HasIndex(x => new { x.CompanyId, x.Status })
             .HasDatabaseName("ix_complaints_company_status");

            e.HasIndex(x => x.SlaDueAt)
             .HasDatabaseName("ix_complaints_sla_due");

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AssignedUser)
             .WithMany()
             .HasForeignKey(x => x.AssignedTo)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Creator)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ComplaintNote ─────────────────────────────────────────────────
        m.Entity<ComplaintNote>(e =>
        {
            e.ToTable("complaint_notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Note).HasColumnType("text").IsRequired();

            e.HasOne(x => x.Complaint)
             .WithMany(c => c.Notes)
             .HasForeignKey(x => x.ComplaintId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Creator)
             .WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──────────────────────────────────────────────────────
        m.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.UserEmail).HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");

            e.HasIndex(x => new { x.CompanyId, x.CreatedAt })
             .HasDatabaseName("ix_audit_logs_company_created");
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt })
             .HasDatabaseName("ix_audit_logs_entity");
            e.HasIndex(x => new { x.UserId, x.CreatedAt })
             .HasDatabaseName("ix_audit_logs_user");

            // AuditLog hiç silinmez — FK restrict + no cascades
            e.HasOne<Company>()
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Branch>()
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne<User>()
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── KvkkConsentLog ─────────────────────────────────────────────────
        m.Entity<KvkkConsentLog>(e =>
        {
            e.ToTable("kvkk_consent_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.ConsentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);

            e.HasIndex(x => new { x.PatientId, x.ConsentType, x.GivenAt })
             .HasDatabaseName("ix_kvkk_consent_patient_type");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DataExportRequest ─────────────────────────────────────────────
        m.Entity<DataExportRequest>(e =>
        {
            e.ToTable("data_export_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.FilePath).HasMaxLength(500);

            e.HasIndex(x => new { x.PatientId, x.Status })
             .HasDatabaseName("ix_data_export_requests_patient_status");

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Requester)
             .WithMany()
             .HasForeignKey(x => x.RequestedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EInvoice ─────────────────────────────────────────────────────
        m.Entity<EInvoice>(e =>
        {
            e.ToTable("einvoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.Series).HasMaxLength(3).HasDefaultValue("GBS");
            e.Property(x => x.EInvoiceNo).HasMaxLength(50);
            e.HasIndex(x => x.EInvoiceNo).IsUnique().HasFilter("\"EInvoiceNo\" IS NOT NULL")
             .HasDatabaseName("ix_einvoices_einvoice_no");

            e.Property(x => x.ReceiverName).HasMaxLength(300).IsRequired();
            e.Property(x => x.ReceiverTc).HasMaxLength(11);
            e.Property(x => x.ReceiverVkn).HasMaxLength(10);
            e.Property(x => x.ReceiverTaxOffice).HasMaxLength(100);
            e.Property(x => x.ReceiverEmail).HasMaxLength(200);

            e.Property(x => x.Subtotal).HasColumnType("numeric(12,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.TaxableAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.TaxRate).HasColumnType("numeric(5,2)").HasDefaultValue(10m);
            e.Property(x => x.TaxAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.Total).HasColumnType("numeric(12,2)");
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.LanguageCode).HasMaxLength(5).HasDefaultValue("tr");

            e.Property(x => x.GibUuid).HasMaxLength(100);
            e.HasIndex(x => x.GibUuid).IsUnique().HasFilter("\"GibUuid\" IS NOT NULL")
             .HasDatabaseName("ix_einvoices_gib_uuid");
            e.Property(x => x.GibStatus).HasMaxLength(50);
            e.Property(x => x.GibResponse).HasColumnType("jsonb");

            e.Property(x => x.PdfPath).HasMaxLength(500);

            e.Property(x => x.InvoiceType)
             .HasConversion<int>();
            e.Property(x => x.ReceiverType)
             .HasConversion<int>();

            e.HasIndex(x => new { x.CompanyId, x.InvoiceDate })
             .HasDatabaseName("ix_einvoices_company_date");

            e.HasOne(x => x.Payment).WithMany()
             .HasForeignKey(x => x.PaymentId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Creator).WithMany()
             .HasForeignKey(x => x.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EInvoiceItem ──────────────────────────────────────────────────
        m.Entity<EInvoiceItem>(e =>
        {
            e.ToTable("einvoice_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(20).HasDefaultValue("Adet");
            e.Property(x => x.Quantity).HasColumnType("numeric(10,3)").HasDefaultValue(1m);
            e.Property(x => x.UnitPrice).HasColumnType("numeric(12,2)");
            e.Property(x => x.DiscountRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
            e.Property(x => x.DiscountAmount).HasColumnType("numeric(12,2)").HasDefaultValue(0m);
            e.Property(x => x.TaxRate).HasColumnType("numeric(5,2)").HasDefaultValue(10m);
            e.Property(x => x.TaxAmount).HasColumnType("numeric(12,2)");
            e.Property(x => x.Total).HasColumnType("numeric(12,2)");

            e.HasOne(x => x.EInvoice).WithMany(i => i.Items)
             .HasForeignKey(x => x.EInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── EInvoiceIntegration ───────────────────────────────────────────
        m.Entity<EInvoiceIntegration>(e =>
        {
            e.ToTable("einvoice_integrations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            e.Property(x => x.Vkn).HasMaxLength(10).IsRequired();
            e.Property(x => x.TaxOffice).HasMaxLength(100).IsRequired();
            e.Property(x => x.CompanyTitle).HasMaxLength(300).IsRequired();
            e.Property(x => x.Config).HasColumnType("jsonb").HasDefaultValue("{}");

            e.HasIndex(x => x.CompanyId)
             .HasDatabaseName("ix_einvoice_integrations_company");
        });

        // ── TranslationKey ────────────────────────────────────────────────
        m.Entity<TranslationKey>(e =>
        {
            e.ToTable("translation_keys");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.Property(x => x.Key).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.Key).IsUnique()
             .HasDatabaseName("ix_translation_keys_key");

            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Category)
             .HasDatabaseName("ix_translation_keys_category");

            e.Property(x => x.Description).HasColumnType("text");
        });

        // ── Translation ───────────────────────────────────────────────────
        m.Entity<Translation>(e =>
        {
            e.ToTable("translations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();

            e.Property(x => x.Value).HasColumnType("text").IsRequired();

            // UNIQUE(key_id, language_id)
            e.HasIndex(x => new { x.KeyId, x.LanguageId }).IsUnique()
             .HasDatabaseName("ix_translations_key_language");

            // Dil kodu üzerinden hızlı filtreleme
            e.HasIndex(x => x.LanguageId)
             .HasDatabaseName("ix_translations_language");

            e.HasOne(x => x.TranslationKey)
             .WithMany(k => k.Translations)
             .HasForeignKey(x => x.KeyId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Language)
             .WithMany()
             .HasForeignKey(x => x.LanguageId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TreatmentCategory ─────────────────────────────────────────────
        m.Entity<TreatmentCategory>(e =>
        {
            e.ToTable("treatment_categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_treatment_categories_public_id");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();

            e.HasIndex(x => new { x.CompanyId, x.IsActive })
             .HasDatabaseName("ix_treatment_categories_company_active");

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Parent)
             .WithMany()
             .HasForeignKey(x => x.ParentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Treatment ─────────────────────────────────────────────────────
        m.Entity<Treatment>(e =>
        {
            e.ToTable("treatments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_treatments_public_id");

            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique()
             .HasDatabaseName("ix_treatments_code");

            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.SutCode).HasMaxLength(20);
            e.Property(x => x.Tags).HasColumnType("jsonb");
            e.Property(x => x.KdvRate).HasColumnType("numeric(5,2)");
            e.Property(x => x.CostPrice).HasColumnType("numeric(12,2)");
            e.Property(x => x.AllowedScopes).HasColumnType("integer[]");
            e.Property(x => x.LabDefaultCategory).HasMaxLength(200);

            e.HasIndex(x => new { x.CompanyId, x.IsActive })
             .HasDatabaseName("ix_treatments_company_active");

            e.HasOne(x => x.Company)
             .WithMany()
             .HasForeignKey(x => x.CompanyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
             .WithMany()
             .HasForeignKey(x => x.CategoryId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PricingRule ───────────────────────────────────────────────────
        m.Entity<PricingRule>(e =>
        {
            e.ToTable("pricing_rules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_pricing_rules_public_id");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.RuleType).HasMaxLength(50).IsRequired();
            e.Property(x => x.IncludeFilters).HasColumnType("jsonb");
            e.Property(x => x.ExcludeFilters).HasColumnType("jsonb");
            e.Property(x => x.Formula).HasColumnType("text");
            e.Property(x => x.OutputCurrency).HasMaxLength(3).HasDefaultValue("TRY");

            e.HasIndex(x => new { x.CompanyId, x.IsActive, x.Priority })
             .HasDatabaseName("ix_pricing_rules_company_active_priority");
        });

        // ── Campaign ─────────────────────────────────────────────────────
        m.Entity<Campaign>(e =>
        {
            e.ToTable("campaigns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_campaigns_public_id");

            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");

            e.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique()
             .HasDatabaseName("ix_campaigns_company_code");

            e.HasIndex(x => new { x.CompanyId, x.IsActive, x.ValidFrom, x.ValidUntil })
             .HasDatabaseName("ix_campaigns_company_active_dates");
        });

        // ── ReferencePriceList ────────────────────────────────────────────
        m.Entity<ReferencePriceList>(e =>
        {
            e.ToTable("reference_price_lists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_reference_price_lists_public_id");

            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.Code, x.Year }).IsUnique()
             .HasDatabaseName("ix_reference_price_lists_code_year");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
        });

        // ── ReferencePriceItem ────────────────────────────────────────────
        m.Entity<ReferencePriceItem>(e =>
        {
            e.ToTable("reference_price_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.TreatmentCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.TreatmentName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Price).HasColumnType("numeric(12,2)");
            e.Property(x => x.PriceKdv).HasColumnType("numeric(12,2)");
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.Metadata).HasColumnType("jsonb");

            e.HasIndex(x => new { x.ListId, x.TreatmentCode })
             .HasDatabaseName("ix_reference_price_items_list_code");

            e.HasOne(x => x.List)
             .WithMany(l => l.Items)
             .HasForeignKey(x => x.ListId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TreatmentMapping ──────────────────────────────────────────────
        m.Entity<TreatmentMapping>(e =>
        {
            e.ToTable("treatment_mappings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.ReferenceCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.MappingQuality).HasMaxLength(20);
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => new { x.InternalTreatmentId, x.ReferenceListId }).IsUnique()
             .HasDatabaseName("ix_treatment_mappings_treatment_list");

            e.HasOne(x => x.InternalTreatment)
             .WithMany()
             .HasForeignKey(x => x.InternalTreatmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ReferenceList)
             .WithMany()
             .HasForeignKey(x => x.ReferenceListId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── User2FASettings ───────────────────────────────────────────────
        m.Entity<User2FASettings>(e =>
        {
            e.ToTable("user_2fa_settings");
            e.HasKey(x => x.UserId);

            e.Property(x => x.TotpSecret).HasColumnType("text");
            e.Property(x => x.PreferredMethod).HasMaxLength(20);
            e.Property(x => x.BackupCodes).HasColumnType("jsonb");
            e.Property(x => x.BackupCodesAt).IsRequired(false);
            e.Property(x => x.TotpVerifiedAt).IsRequired(false);
            e.Property(x => x.UpdatedAt).IsRequired();

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TrustedDevice ─────────────────────────────────────────────────
        m.Entity<TrustedDevice>(e =>
        {
            e.ToTable("trusted_devices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.DeviceToken).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.DeviceToken).IsUnique()
             .HasDatabaseName("ix_trusted_devices_token");

            e.Property(x => x.DeviceName).HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(45);

            e.HasIndex(x => new { x.UserId, x.ExpiresAt })
             .HasDatabaseName("ix_trusted_devices_user_expires");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BranchSecurityPolicy ──────────────────────────────────────────
        m.Entity<BranchSecurityPolicy>(e =>
        {
            e.ToTable("branch_security_policies");
            e.HasKey(x => x.BranchId);

            e.Property(x => x.AllowedIpRanges).HasColumnType("jsonb");
            e.Property(x => x.SessionTimeoutMinutes).HasDefaultValue(480);
            e.Property(x => x.MaxFailedAttempts).HasDefaultValue(5);
            e.Property(x => x.LockoutMinutes).HasDefaultValue(30);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BackupLog ─────────────────────────────────────────────────────
        m.Entity<BackupLog>(e =>
        {
            e.ToTable("backup_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_backup_logs_public_id");

            e.Property(x => x.BackupType).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(500);
            e.Property(x => x.FileSizeMb).HasColumnType("numeric(10,2)");
            e.Property(x => x.StorageLocation).HasMaxLength(1000);
            e.Property(x => x.Checksum).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("started").IsRequired();
            e.Property(x => x.ErrorMessage).HasColumnType("text");

            e.HasIndex(x => new { x.Status, x.StartedAt })
             .HasDatabaseName("ix_backup_logs_status_started");

            e.HasIndex(x => x.CompanyId)
             .HasDatabaseName("ix_backup_logs_company");
        });

        ConfigureLaboratoryEntities(m);
    }

    // ─── Laboratuvar ───────────────────────────────────────────────────────
    private static void ConfigureLaboratoryEntities(ModelBuilder m)
    {
        // Laboratory
        m.Entity<Laboratory>(e =>
        {
            e.ToTable("laboratories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_laboratories_public_id");

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Website).HasMaxLength(200);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.District).HasMaxLength(100);
            e.Property(x => x.Address).HasColumnType("text");
            e.Property(x => x.ContactPerson).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(30);
            e.Property(x => x.WorkingDays).HasColumnType("jsonb");
            e.Property(x => x.WorkingHours).HasMaxLength(100);
            e.Property(x => x.PaymentTerms).HasColumnType("text");
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => new { x.CompanyId, x.IsActive })
             .HasDatabaseName("ix_laboratories_company_active");

            e.HasIndex(x => new { x.CompanyId, x.Code })
             .IsUnique()
             .HasFilter("\"Code\" IS NOT NULL")
             .HasDatabaseName("ix_laboratories_company_code");
        });

        // LaboratoryBranchAssignment
        m.Entity<LaboratoryBranchAssignment>(e =>
        {
            e.ToTable("laboratory_branch_assignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.HasIndex(x => new { x.LaboratoryId, x.BranchId }).IsUnique()
             .HasDatabaseName("ix_lab_branch_unique");

            e.HasIndex(x => x.BranchId).HasDatabaseName("ix_lab_branch_branch");
            e.HasIndex(x => x.LaboratoryId).HasDatabaseName("ix_lab_branch_lab");

            e.HasOne(x => x.Laboratory)
             .WithMany()
             .HasForeignKey(x => x.LaboratoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // LaboratoryPriceItem
        m.Entity<LaboratoryPriceItem>(e =>
        {
            e.ToTable("laboratory_price_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_lab_price_items_public_id");

            e.Property(x => x.ItemName).HasMaxLength(500).IsRequired();
            e.Property(x => x.ItemCode).HasMaxLength(100);
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.Price).HasColumnType("numeric(12,2)");
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.PricingType).HasMaxLength(50);
            e.Property(x => x.Category).HasMaxLength(200);

            e.HasIndex(x => x.LaboratoryId).HasDatabaseName("ix_lab_prices_lab");
            e.HasIndex(x => x.Category).HasDatabaseName("ix_lab_prices_category");

            e.HasOne(x => x.Laboratory)
             .WithMany()
             .HasForeignKey(x => x.LaboratoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // LaboratoryWork
        m.Entity<LaboratoryWork>(e =>
        {
            e.ToTable("laboratory_works");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique()
             .HasDatabaseName("ix_lab_works_public_id");

            e.Property(x => x.WorkNo).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.WorkNo }).IsUnique()
             .HasDatabaseName("ix_lab_works_company_no");

            e.Property(x => x.WorkType).HasMaxLength(50).IsRequired();
            e.Property(x => x.DeliveryType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ToothNumbers).HasMaxLength(200);
            e.Property(x => x.ShadeColor).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.TotalCost).HasColumnType("numeric(12,2)");
            e.Property(x => x.CostDetails).HasColumnType("jsonb");
            e.Property(x => x.Attachments).HasColumnType("jsonb");
            e.Property(x => x.DoctorNotes).HasColumnType("text");
            e.Property(x => x.LabNotes).HasColumnType("text");
            e.Property(x => x.ApprovalNotes).HasColumnType("text");

            e.HasIndex(x => x.PatientId).HasDatabaseName("ix_lab_works_patient");
            e.HasIndex(x => x.DoctorId).HasDatabaseName("ix_lab_works_doctor");
            e.HasIndex(x => x.LaboratoryId).HasDatabaseName("ix_lab_works_lab");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_lab_works_status");
            e.HasIndex(x => new { x.BranchId, x.Status })
             .HasDatabaseName("ix_lab_works_branch_status");
            e.HasIndex(x => new { x.SentToLabAt, x.EstimatedDeliveryDate })
             .HasDatabaseName("ix_lab_works_dates");

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Patient)
             .WithMany()
             .HasForeignKey(x => x.PatientId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Doctor)
             .WithMany()
             .HasForeignKey(x => x.DoctorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Laboratory)
             .WithMany()
             .HasForeignKey(x => x.LaboratoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.TreatmentPlanItem)
             .WithMany()
             .HasForeignKey(x => x.TreatmentPlanItemId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(x => x.Items)
             .WithOne(x => x.Work)
             .HasForeignKey(x => x.WorkId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.History)
             .WithOne(x => x.Work)
             .HasForeignKey(x => x.WorkId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // LaboratoryWorkItem
        m.Entity<LaboratoryWorkItem>(e =>
        {
            e.ToTable("laboratory_work_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.ItemName).HasMaxLength(500).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("numeric(12,2)");
            e.Property(x => x.TotalPrice).HasColumnType("numeric(12,2)");
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => x.WorkId).HasDatabaseName("ix_lab_work_items_work");

            e.HasOne(x => x.LabPriceItem)
             .WithMany()
             .HasForeignKey(x => x.LabPriceItemId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // LaboratoryWorkHistory
        m.Entity<LaboratoryWorkHistory>(e =>
        {
            e.ToTable("laboratory_work_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.Property(x => x.OldStatus).HasMaxLength(50);
            e.Property(x => x.NewStatus).HasMaxLength(50).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");

            e.HasIndex(x => x.WorkId).HasDatabaseName("ix_lab_history_work");
        });

        // LaboratoryApprovalAuthority
        m.Entity<LaboratoryApprovalAuthority>(e =>
        {
            e.ToTable("laboratory_approval_authorities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");

            e.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique()
             .HasDatabaseName("ix_lab_approval_user_branch");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);
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
                typeof(OutboxMessage),
                typeof(LoginAttempt),
                typeof(RefreshToken),
                // Notification: bildirim arşivi kalıcıdır, soft-delete uygulanmaz
                typeof(Notification),
                // PatientAnamnesis/Note/File: kendi deleted_at mantığını kullanır
                typeof(PatientAnamnesis),
                typeof(PatientNote),
                typeof(PatientFile),
                // OnlineBookingRequest: kendi status akışını yönetir (cancelled ile)
                typeof(OnlineBookingRequest),
                // PatientPortalAccount: portal hesabı is_active ile yönetilir
                typeof(PatientPortalAccount),
                // Audit / KVKK: bu tablolar hiçbir zaman soft-delete almaz
                typeof(AuditLog),
                typeof(KvkkConsentLog),
                typeof(DataExportRequest),
                // Laboratuvar: iş emri geçmişi append-only, iş kalemleri cascade siliniyor
                typeof(LaboratoryWorkHistory),
                typeof(LaboratoryWorkItem)
                // PatientMedication, PaymentAllocation, DoctorCommission, SmsQueue,
                // ToothConditionHistory, DoctorOnlineBookingSettings, DoctorOnlineSchedule,
                // DoctorOnlineBlock, BranchOnlineBookingSettings, TranslationKey, Translation,
                // EInvoice, EInvoiceItem, EInvoiceIntegration
                // BaseEntity türemediğinden bu döngüde zaten işlenmez.
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

        // ── Döviz tabloları ───────────────────────────────────────────────
        ConfigureExchangeRates(modelBuilder);
    }

    private static void ConfigureExchangeRates(ModelBuilder m)
    {
        // ExchangeRate
        m.Entity<ExchangeRate>(e =>
        {
            e.ToTable("exchange_rates");
            e.HasKey(x => x.Id);
            e.Property(x => x.FromCurrency).HasMaxLength(3).IsRequired();
            e.Property(x => x.ToCurrency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Rate).HasColumnType("numeric(18,6)").IsRequired();
            e.Property(x => x.Source).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.RateDate })
             .IsUnique()
             .HasDatabaseName("IX_exchange_rates_from_to_date");
            e.HasIndex(x => x.RateDate)
             .HasDatabaseName("IX_exchange_rates_date");
        });

        // ExchangeRateOverride
        m.Entity<ExchangeRateOverride>(e =>
        {
            e.ToTable("exchange_rate_overrides");
            e.HasKey(x => x.Id);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Rate).HasColumnType("numeric(18,6)").IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Currency, x.ValidFrom })
             .HasDatabaseName("IX_exchange_rate_overrides_lookup");
        });

        // ExchangeRateDifference
        m.Entity<ExchangeRateDifference>(e =>
        {
            e.ToTable("exchange_rate_differences");
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.OriginalRate).HasColumnType("numeric(18,6)").IsRequired();
            e.Property(x => x.ActualRate).HasColumnType("numeric(18,6)").IsRequired();
            e.Property(x => x.ForeignAmount).HasColumnType("numeric(18,4)").IsRequired();
            e.Property(x => x.DifferenceAmount).HasColumnType("numeric(18,4)").IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SourceType, x.SourceId })
             .HasDatabaseName("IX_exchange_rate_diffs_source");
            e.HasIndex(x => x.RecordedAt)
             .HasDatabaseName("IX_exchange_rate_diffs_recorded");
        });

        // Payment: yeni sütunlar
        m.Entity<Payment>(e =>
        {
            e.Property(x => x.ExchangeRate).HasColumnType("numeric(18,6)").HasDefaultValue(1m);
            e.Property(x => x.BaseAmount).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
        });

        // EInvoice: yeni sütunlar
        m.Entity<EInvoice>(e =>
        {
            e.Property(x => x.ExchangeRate).HasColumnType("numeric(18,6)").HasDefaultValue(1m);
            e.Property(x => x.BaseAmount).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
        });

        // DoctorCommission: yeni sütunlar
        m.Entity<DoctorCommission>(e =>
        {
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.ExchangeRate).HasColumnType("numeric(18,6)").HasDefaultValue(1m);
            e.Property(x => x.BaseAmount).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
        });

        // TreatmentPlanItem: yeni sütunlar
        m.Entity<TreatmentPlanItem>(e =>
        {
            e.Property(x => x.PriceCurrency).HasMaxLength(3).HasDefaultValue("TRY");
            e.Property(x => x.PriceExchangeRate).HasColumnType("numeric(18,6)").HasDefaultValue(1m);
            e.Property(x => x.PriceBaseAmount).HasColumnType("numeric(18,4)").HasDefaultValue(0m);
            e.Property(x => x.RateLockType).HasDefaultValue(1);
        });
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
