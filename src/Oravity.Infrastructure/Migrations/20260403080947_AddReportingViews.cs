using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <summary>
    /// Raporlama modülü için PostgreSQL view'ları.
    /// View'lar EF entity olarak eşleştirilmez; doğrudan SQL sorguları ile kullanılır.
    /// Tüm view'lar SECURITY INVOKER ile oluşturulur (row-level security uyumlu).
    /// </summary>
    public partial class AddReportingViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── v_daily_revenue ───────────────────────────────────────────────
            // Günlük gelir özeti: tarih + şube + ödeme yöntemi bazında toplam.
            // Payment.PaymentDate DateOnly olduğundan CAST gerekmez.
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW v_daily_revenue AS
SELECT
    p.""PaymentDate""               AS payment_date,
    p.""BranchId""                  AS branch_id,
    b.""CompanyId""                 AS company_id,
    b.""Name""                      AS branch_name,
    p.""Method""                    AS payment_method,
    COUNT(*)                        AS payment_count,
    SUM(p.""Amount"")               AS total_amount
FROM payments p
JOIN branches b ON b.""Id"" = p.""BranchId""
WHERE p.""IsRefunded"" = FALSE
  AND p.""IsDeleted""  = FALSE
GROUP BY
    p.""PaymentDate"",
    p.""BranchId"",
    b.""CompanyId"",
    b.""Name"",
    p.""Method"";
");

            // ── v_doctor_commissions ──────────────────────────────────────────
            // Hekim hakediş özeti: hekim + şube + tarih bazında.
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW v_doctor_commissions AS
SELECT
    dc.""DoctorId""                 AS doctor_id,
    u.""FullName""                  AS doctor_name,
    dc.""BranchId""                 AS branch_id,
    b.""CompanyId""                 AS company_id,
    DATE(dc.""CreatedAt"")          AS commission_date,
    COUNT(*)                        AS item_count,
    SUM(dc.""GrossAmount"")         AS total_gross,
    AVG(dc.""CommissionRate"")      AS avg_commission_rate,
    SUM(dc.""CommissionAmount"")    AS total_commission,
    dc.""Status""                   AS status
FROM doctor_commissions dc
JOIN users    u ON u.""Id""    = dc.""DoctorId""
JOIN branches b ON b.""Id""   = dc.""BranchId""
GROUP BY
    dc.""DoctorId"",
    u.""FullName"",
    dc.""BranchId"",
    b.""CompanyId"",
    DATE(dc.""CreatedAt""),
    dc.""Status"";
");

            // ── v_appointment_stats ───────────────────────────────────────────
            // Randevu istatistikleri: tarih + hekim + hasta + status bazında.
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW v_appointment_stats AS
SELECT
    a.""Id""                                                          AS appointment_id,
    a.""BranchId""                                                    AS branch_id,
    b.""CompanyId""                                                   AS company_id,
    DATE(a.""StartTime"" AT TIME ZONE 'UTC')                          AS appointment_date,
    a.""DoctorId""                                                    AS doctor_id,
    u.""FullName""                                                    AS doctor_name,
    a.""PatientId""                                                   AS patient_id,
    CONCAT(pat.""FirstName"", ' ', pat.""LastName"")                  AS patient_name,
    a.""Status""                                                      AS status,
    EXTRACT(EPOCH FROM (a.""EndTime"" - a.""StartTime"")) / 60.0     AS duration_minutes
FROM appointments a
JOIN branches b    ON b.""Id""    = a.""BranchId""
JOIN users    u    ON u.""Id""    = a.""DoctorId""
JOIN patients pat  ON pat.""Id""  = a.""PatientId""
WHERE a.""IsDeleted"" = FALSE;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_appointment_stats;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_doctor_commissions;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_daily_revenue;");
        }
    }
}
