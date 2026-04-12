using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Appointment.Application;
using Oravity.Core.Modules.Visit.Application;
using Oravity.Core.Modules.Visit.Application.Queries;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Commands;

public record CompleteProtocolCommand(Guid ProtocolPublicId) : IRequest<ProtocolDetailResponse>;

public class CompleteProtocolCommandHandler : IRequestHandler<CompleteProtocolCommand, ProtocolDetailResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICalendarBroadcastService _broadcast;

    public CompleteProtocolCommandHandler(AppDbContext db, ITenantContext tenant, ICalendarBroadcastService broadcast)
    {
        _db        = db;
        _tenant    = tenant;
        _broadcast = broadcast;
    }

    public async Task<ProtocolDetailResponse> Handle(CompleteProtocolCommand request, CancellationToken ct)
    {
        var protocol = await _db.Protocols
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProtocolPublicId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        var patientName = protocol.Patient is { } pp
            ? $"{pp.FirstName} {pp.LastName}".Trim()
            : "";

        // Sadece protokolü oluşturan hekim veya yetkili tamamlayabilir
        if (protocol.DoctorId != _tenant.UserId && !_tenant.IsCompanyAdmin && !_tenant.IsPlatformAdmin)
            throw new ForbiddenException("Bu protokolü tamamlama yetkiniz yok.");

        protocol.Complete();
        await _db.SaveChangesAsync(ct);

        await _broadcast.BroadcastProtocolAsync(
            protocol.BranchId,
            new ProtocolBroadcastDto(
                protocol.PublicId,
                protocol.BranchId,
                protocol.VisitId,
                protocol.PatientId,
                patientName,
                protocol.DoctorId,
                protocol.Doctor?.FullName ?? "",
                protocol.ProtocolNo,
                (int)protocol.ProtocolType,
                (int)protocol.Status),
            CalendarEventType.ProtocolCompleted, ct);

        return await new GetProtocolDetailQueryHandler(_db)
            .Handle(new GetProtocolDetailQuery(request.ProtocolPublicId), ct);
    }
}
