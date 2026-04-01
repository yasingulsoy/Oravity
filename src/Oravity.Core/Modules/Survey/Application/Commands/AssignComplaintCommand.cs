using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Notification.Application.Commands;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record AssignComplaintCommand(
    Guid ComplaintPublicId,
    long AssignedToUserId
) : IRequest<ComplaintResponse>;

public class AssignComplaintCommandHandler
    : IRequestHandler<AssignComplaintCommand, ComplaintResponse>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public AssignComplaintCommandHandler(AppDbContext db, IMediator mediator)
    {
        _db       = db;
        _mediator = mediator;
    }

    public async Task<ComplaintResponse> Handle(
        AssignComplaintCommand request,
        CancellationToken cancellationToken)
    {
        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.PublicId == request.ComplaintPublicId, cancellationToken)
            ?? throw new NotFoundException($"Şikayet bulunamadı: {request.ComplaintPublicId}");

        complaint.Assign(request.AssignedToUserId);
        await _db.SaveChangesAsync(cancellationToken);

        // Atanan kişiye bildirim gönder
        await _mediator.Send(new SendInAppNotificationCommand(
            BranchId:          complaint.BranchId,
            Type:              NotificationType.GeneralInfo,
            Title:             "Şikayet Atandı",
            Message:           $"Size yeni bir şikayet atandı: {complaint.Subject}",
            ToUserId:          request.AssignedToUserId,
            CompanyId:         complaint.CompanyId,
            IsUrgent:          complaint.Priority == ComplaintPriority.Urgent,
            RelatedEntityType: "Complaint",
            RelatedEntityId:   complaint.Id),
            cancellationToken);

        return SurveyMappings.ToResponse(complaint);
    }
}
