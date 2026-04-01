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

    // ─── Appointments ──────────────────────────────────────────────────────
    public DbSet<Appointment> Appointments => Set<Appointment>();

    // ─── Treatment Plans ───────────────────────────────────────────────────
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();

    // ─── Finance ───────────────────────────────────────────────────────────
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<DoctorCommission> DoctorCommissions => Set<DoctorCommission>();

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

        // ── Patient ───────────────────────────────────────────────────────
        m.Entity<Patient>(e =>
        {
            e.ToTable("patients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_patients_public_id");

            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();

            // Ad + Soyad composite index (arama)
            e.HasIndex(x => new { x.BranchId, x.LastName, x.FirstName })
             .HasDatabaseName("ix_patients_branch_name");

            e.Property(x => x.Gender).HasMaxLength(10);
            e.Property(x => x.TcNumberEncrypted).HasMaxLength(500);
            e.Property(x => x.TcNumberHash).HasMaxLength(64);
            e.HasIndex(x => x.TcNumberHash).HasDatabaseName("ix_patients_tc_hash");

            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => new { x.BranchId, x.Phone }).HasDatabaseName("ix_patients_branch_phone");

            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasColumnType("text");
            e.Property(x => x.BloodType).HasMaxLength(5);
            e.Property(x => x.PreferredLanguageCode).HasMaxLength(5).HasDefaultValue("tr");
            e.Property(x => x.IsActive).HasDefaultValue(true);

            // Audit fields
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.CreatedByUserId);
            e.Property(x => x.UpdatedByUserId);

            e.HasOne(x => x.Branch)
             .WithMany()
             .HasForeignKey(x => x.BranchId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Appointment ───────────────────────────────────────────────────
        m.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ix_appointments_public_id");

            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.StartTime).IsRequired();
            e.Property(x => x.EndTime).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.RowVersion).HasDefaultValue(1).IsConcurrencyToken();

            // Optimistic lock: doctor+branch+start_time unique (aktif randevular)
            // Status 6=İptal, 7=Gelmedi hariç tutulur
            e.HasIndex(x => new { x.DoctorId, x.BranchId, x.StartTime })
             .IsUnique()
             .HasFilter("\"Status\" NOT IN (6, 7)")
             .HasDatabaseName("ix_appointments_slot_unique");

            // Sorgu index'leri
            e.HasIndex(x => new { x.BranchId, x.StartTime })
             .HasDatabaseName("ix_appointments_branch_start");
            e.HasIndex(x => new { x.DoctorId, x.StartTime })
             .HasDatabaseName("ix_appointments_doctor_start");

            // Audit fields
            e.Property(x => x.TenantId).IsRequired();

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

            // TreatmentId: treatments tablosu henüz implement edilmedi — FK constraint yok
            e.HasIndex(x => x.PlanId).HasDatabaseName("ix_treatment_plan_items_plan");
            e.HasIndex(x => x.TreatmentId).HasDatabaseName("ix_treatment_plan_items_treatment");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_treatment_plan_items_status");

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

            e.HasIndex(x => x.PaymentId).HasDatabaseName("ix_payment_alloc_payment");
            e.HasIndex(x => x.TreatmentPlanItemId).HasDatabaseName("ix_payment_alloc_item");

            e.HasOne(x => x.TreatmentPlanItem)
             .WithMany()
             .HasForeignKey(x => x.TreatmentPlanItemId)
             .OnDelete(DeleteBehavior.Restrict);
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

            e.HasIndex(x => x.DoctorId).HasDatabaseName("ix_doctor_commission_doctor");
            e.HasIndex(x => x.TreatmentPlanItemId).HasDatabaseName("ix_doctor_commission_item");
            e.HasIndex(x => new { x.BranchId, x.Status }).HasDatabaseName("ix_doctor_commission_branch_status");

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

            // Hasta başına tek kayıt
            e.HasIndex(x => x.PatientId).IsUnique()
             .HasDatabaseName("ix_patient_anamnesis_patient_unique");

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
             .HasFilter("patient_id IS NOT NULL");

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
                typeof(PatientPortalAccount)
                // PatientMedication, PaymentAllocation, DoctorCommission, SmsQueue,
                // ToothConditionHistory, DoctorOnlineBookingSettings, DoctorOnlineSchedule,
                // DoctorOnlineBlock, BranchOnlineBookingSettings BaseEntity türemediğinden
                // bu döngüde zaten işlenmez.
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
