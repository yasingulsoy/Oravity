using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Notification.Application;
using Oravity.Core.Modules.Notification.Infrastructure.Services;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using NotifEntity = Oravity.SharedKernel.Entities.Notification;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

/// <summary>
/// Hekim hastayı çağırır.
/// – Açık, henüz başlatılmamış protokol varsa → başlatır (StartedAt set eder).
/// – Yoksa → resepsiyon rolüne "protokol aç ve odaya gönder" bildirimi gönderir.
/// </summary>
public record RequestPatientCallCommand(Guid AppointmentPublicId) : IRequest<RequestPatientCallResult>;

public record RequestPatientCallResult(bool ProtocolStarted, string PatientName);

public class RequestPatientCallCommandHandler
    : IRequestHandler<RequestPatientCallCommand, RequestPatientCallResult>
{
    private readonly AppDbContext              _db;
    private readonly ITenantContext            _tenant;
    private readonly INotificationHubService   _notifHub;
    private readonly ICalendarBroadcastService _calendarBroadcast;

    public RequestPatientCallCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        INotificationHubService notifHub,
        ICalendarBroadcastService calendarBroadcast)
    {
        _db                = db;
        _tenant            = tenant;
        _notifHub          = notifHub;
        _calendarBroadcast = calendarBroadcast;
    }

    public async Task<RequestPatientCallResult> Handle(
        RequestPatientCallCommand request,
        CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Branch)
            .FirstOrDefaultAsync(a => a.PublicId == request.AppointmentPublicId, ct)
            ?? throw new NotFoundException("Randevu bulunamadı.");

        var patientName = appointment.Patient is { } p
            ? $"{p.FirstName} {p.LastName}".Trim()
            : "Hasta";

        // Çağıran hekimin adını al
        var doctor = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _tenant.UserId, ct);
        var doctorName = doctor?.FullName ?? "Hekim";

        // Bu randevuya ait vizite ve açık protokol var mı?
        var visit = await _db.Visits
            .Include(v => v.Protocols)
            .FirstOrDefaultAsync(
                v => v.AppointmentId == appointment.Id && !v.IsDeleted, ct);

        if (visit != null)
        {
            var openProtocol = visit.Protocols
                .FirstOrDefault(p =>
                    (int)p.Status == (int)ProtocolStatus.Open
                    && !p.StartedAt.HasValue
                    && !p.IsDeleted);

            if (openProtocol != null)
            {
                openProtocol.Start();
                visit.MarkCalled();
                await _db.SaveChangesAsync(ct);
                await BroadcastVisitCalledAsync(visit, patientName, ct);
                return new RequestPatientCallResult(true, patientName);
            }

            // Açık protokol yok ama visit var → çağrıyı işaretle
            visit.MarkCalled();
        }

        // Açık protokol yok → resepsiyona bildirim gönder
        var companyId = appointment.Branch?.CompanyId ?? _tenant.CompanyId;

        var notification = NotifEntity.Create(
            branchId:          appointment.BranchId,
            type:              NotificationType.DoctorMessage,
            title:             "Hasta Çağrısı",
            message:           $"{doctorName}, {patientName} hastasını bekliyor. Lütfen bekleme listesinden protokol açarak odaya yönlendirin.",
            toRole:            1, // 1 = Resepsiyon
            companyId:         companyId,
            isUrgent:          true,
            relatedEntityType: "Appointment",
            relatedEntityId:   appointment.Id);

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _notifHub.SendToBranch(
            appointment.BranchId,
            new NotificationPayload(
                notification.PublicId,
                notification.Type,
                NotificationMappings.TypeLabel(notification.Type),
                notification.Title,
                notification.Message,
                notification.IsUrgent,
                notification.RelatedEntityType,
                notification.RelatedEntityId,
                notification.CreatedAt),
            ct);

        // Waiting list'te isBeingCalled göstermek için VisitUpdated yayınla
        if (visit != null)
            await BroadcastVisitCalledAsync(visit, patientName, ct);

        return new RequestPatientCallResult(false, patientName);
    }

    private Task BroadcastVisitCalledAsync(SharedKernel.Entities.Visit visit, string patientName, CancellationToken ct)
        => _calendarBroadcast.BroadcastVisitAsync(
            visit.BranchId,
            new VisitBroadcastDto(visit.PublicId, visit.BranchId, visit.PatientId, patientName, visit.IsWalkIn, (int)visit.Status),
            CalendarEventType.Updated,
            ct);
}
