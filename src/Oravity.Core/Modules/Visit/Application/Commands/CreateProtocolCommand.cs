using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record CreateProtocolCommand(
    Guid VisitPublicId,
    long DoctorId,
    int  ProtocolType  // 1=Muayene, 2=Tedavi, 3=Konsültasyon, 4=Kontrol, 5=Acil
) : IRequest<ProtocolDetailResponse>;

public class CreateProtocolCommandHandler : IRequestHandler<CreateProtocolCommand, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CreateProtocolCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<ProtocolDetailResponse> Handle(CreateProtocolCommand request, CancellationToken ct)
    {
        var visit = await _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Branch)
            .Include(v => v.Protocols)
            .Include(v => v.Appointment)
            .FirstOrDefaultAsync(v => v.PublicId == request.VisitPublicId && !v.IsDeleted, ct)
            ?? throw new NotFoundException("Vizite bulunamadı.");

        var patientName = visit.Patient is { } vp
            ? $"{vp.FirstName} {vp.LastName}".Trim()
            : "";

        if (visit.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş viziteye protokol eklenemez.");

        // 1 ziyaret = 1 protokol kuralı
        var hasActiveProtocol = visit.Protocols.Any(p => p.Status != ProtocolStatus.Cancelled && !p.IsDeleted);
        if (hasActiveProtocol)
            throw new InvalidOperationException("Bu ziyaret için zaten bir protokol açılmış.");

        // Randevulu hastalarda hekim randevunun hekiminden belirlenir; walk-in'de istek parametresi kullanılır
        var resolvedDoctorId = visit.Appointment?.DoctorId ?? request.DoctorId;

        var doctor = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == resolvedDoctorId && u.IsActive, ct)
            ?? throw new NotFoundException("Hekim bulunamadı.");

        var companyId = visit.Branch?.CompanyId ?? _tenant.CompanyId
            ?? throw new ForbiddenException("Şirket bağlamı gereklidir.");

        // ─── Sıra no al (FOR UPDATE ile race condition önle) ──────────────
        var year = DateTime.UtcNow.Year;

        var seq = await _db.ProtocolSequences
            .FirstOrDefaultAsync(s => s.BranchId == visit.BranchId && s.Year == year, ct);

        if (seq is null)
        {
            seq = ProtocolSequence.Create(visit.BranchId, year);
            _db.ProtocolSequences.Add(seq);
            await _db.SaveChangesAsync(ct);  // composite PK önce kaydet
        }

        var nextSeq = seq.Increment();

        var protocol = Protocol.Create(
            visitId:   visit.Id,
            branchId:  visit.BranchId,
            companyId: companyId,
            patientId: visit.PatientId,
            doctorId:  resolvedDoctorId,
            type:      (ProtocolType)request.ProtocolType,
            year:      year,
            seq:       nextSeq,
            createdBy: _tenant.UserId);

        _db.Protocols.Add(protocol);
        visit.OpenProtocol();

        await _db.SaveChangesAsync(ct);

        // SignalR
        await _broadcast.BroadcastProtocolAsync(
            visit.BranchId,
            new ProtocolBroadcastDto(
                protocol.PublicId,
                protocol.BranchId,
                visit.Id,
                visit.PatientId,
                patientName,
                resolvedDoctorId,
                doctor.FullName,
                protocol.ProtocolNo,
                (int)protocol.ProtocolType,
                (int)protocol.Status),
            CalendarEventType.ProtocolCreated, ct);

        return await new GetProtocolDetailQueryHandler(_db)
            .Handle(new GetProtocolDetailQuery(protocol.PublicId), ct);
    }
}
