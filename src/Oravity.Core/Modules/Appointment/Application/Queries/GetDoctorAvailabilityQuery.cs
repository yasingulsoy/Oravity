using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Appointment.Application.Queries;

public record GetDoctorAvailabilityQuery(
    long DoctorId,
    DateOnly Date,
    int SlotDurationMinutes = 30,
    /// <summary>Gün başlangıcı (saat, yerel UTC offset'siz). Örn: 8 = 08:00</summary>
    int WorkdayStartHour = 8,
    /// <summary>Gün bitişi. Örn: 18 = 18:00</summary>
    int WorkdayEndHour = 18
) : IRequest<IReadOnlyList<TimeSlotDto>>;

public class GetDoctorAvailabilityQueryHandler
    : IRequestHandler<GetDoctorAvailabilityQuery, IReadOnlyList<TimeSlotDto>>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetDoctorAvailabilityQueryHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TimeSlotDto>> Handle(
        GetDoctorAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var branchId = _tenant.BranchId
            ?? _tenant.CompanyId;  // fallback for company admin

        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd   = dayStart.AddDays(1);

        // Mevcut randevuları getir (iptal/gelmedi dahil edilmez)
        var existingQuery = _db.Appointments.AsNoTracking()
            .Where(a =>
                a.DoctorId  == request.DoctorId &&
                a.StartTime >= dayStart &&
                a.StartTime <  dayEnd &&
                a.Status    != AppointmentStatus.Cancelled &&
                a.Status    != AppointmentStatus.NoShow);

        if (branchId.HasValue)
            existingQuery = existingQuery.Where(a => a.BranchId == branchId.Value);

        var busySlots = await existingQuery
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(cancellationToken);

        // Çalışma saati aralığında tüm slotları üret
        var workStart = dayStart.AddHours(request.WorkdayStartHour);
        var workEnd   = dayStart.AddHours(request.WorkdayEndHour);
        var duration  = TimeSpan.FromMinutes(request.SlotDurationMinutes);

        var slots = new List<TimeSlotDto>();
        var current = workStart;

        while (current.Add(duration) <= workEnd)
        {
            var slotEnd  = current.Add(duration);
            var isBusy   = busySlots.Any(b => b.StartTime < slotEnd && b.EndTime > current);
            slots.Add(new TimeSlotDto(current, slotEnd, !isBusy));
            current = slotEnd;
        }

        return slots;
    }
}
