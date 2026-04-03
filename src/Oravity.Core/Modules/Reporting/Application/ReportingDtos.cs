using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Reporting.Application;

// ─── Dashboard ────────────────────────────────────────────────────────────
public record DashboardSummary(
    AppointmentTodaySummary     Appointments,
    RevenueTodaySummary         Revenue,
    int                         PendingBookingRequests,
    int                         UnreadNotifications,
    DateTime                    GeneratedAt);

public record AppointmentTodaySummary(
    int Total,
    int Completed,
    int Pending,
    int NoShow,
    int Cancelled);

public record RevenueTodaySummary(
    decimal                         Total,
    IReadOnlyList<RevenueByMethod>  ByMethod);

public record RevenueByMethod(
    string  Method,
    decimal Amount,
    int     Count);

// ─── Daily Revenue ────────────────────────────────────────────────────────
public record DailyRevenueReport(
    DateTime                         StartDate,
    DateTime                         EndDate,
    decimal                          GrandTotal,
    IReadOnlyList<DailyRevenueLine>  ByDay,
    IReadOnlyList<RevenueByMethod>   ByMethod,
    IReadOnlyList<RevenueByDoctor>   ByDoctor);

public record DailyRevenueLine(
    DateOnly Date,
    decimal  Total,
    int      PaymentCount);

public record RevenueByDoctor(
    long    DoctorId,
    string  DoctorName,
    decimal Total,
    int     PaymentCount);

// ─── Doctor Performance ───────────────────────────────────────────────────
public record DoctorPerformanceReport(
    DateTime                              StartDate,
    DateTime                              EndDate,
    IReadOnlyList<DoctorPerformanceLine>  Doctors);

public record DoctorPerformanceLine(
    long    DoctorId,
    string  DoctorName,
    int     CompletedAppointments,
    int     CompletedTreatmentItems,
    decimal TotalRevenue,
    decimal TotalCommission,
    decimal CommissionRate);

// ─── Appointment Stats ────────────────────────────────────────────────────
public record AppointmentStatsReport(
    DateTime                                  StartDate,
    DateTime                                  EndDate,
    int                                       Total,
    decimal                                   NoShowRate,
    int                                       AvgDurationMinutes,
    IReadOnlyList<AppointmentStatusSummary>   ByStatus,
    IReadOnlyList<AppointmentByDayLine>       ByDay);

public record AppointmentStatusSummary(
    AppointmentStatus Status,
    string            Label,
    int               Count,
    decimal           Percentage);

public record AppointmentByDayLine(
    DateOnly Date,
    int      Total,
    int      Completed,
    int      NoShow);

// ─── Patient Stats ────────────────────────────────────────────────────────
public record PatientStatsReport(
    DateTime                              StartDate,
    DateTime                              EndDate,
    int                                   NewPatients,
    int                                   TotalActivePatients,
    IReadOnlyList<TopPatientLine>         TopPatients);

public record TopPatientLine(
    long    PatientId,
    Guid    PublicId,
    string  FullName,
    int     TreatmentItemCount,
    decimal TotalPaid);
