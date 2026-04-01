using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record UpdateComplaintStatusCommand(
    Guid ComplaintPublicId,
    ComplaintStatus NewStatus,
    string? Resolution = null
) : IRequest<ComplaintResponse>;

public class UpdateComplaintStatusCommandHandler
    : IRequestHandler<UpdateComplaintStatusCommand, ComplaintResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateComplaintStatusCommandHandler(AppDbContext db, ICurrentUser user)
    {
        _db   = db;
        _user = user;
    }

    public async Task<ComplaintResponse> Handle(
        UpdateComplaintStatusCommand request,
        CancellationToken cancellationToken)
    {
        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.PublicId == request.ComplaintPublicId, cancellationToken)
            ?? throw new NotFoundException($"Şikayet bulunamadı: {request.ComplaintPublicId}");

        if (request.NewStatus == ComplaintStatus.Resolved)
        {
            if (string.IsNullOrWhiteSpace(request.Resolution))
                throw new InvalidOperationException("Çözüm açıklaması zorunludur.");
            complaint.Resolve(request.Resolution!, _user.UserId);
        }
        else if (request.NewStatus == ComplaintStatus.Closed)
        {
            complaint.Close();
        }
        else
        {
            complaint.UpdateStatus(request.NewStatus);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SurveyMappings.ToResponse(complaint);
    }
}
