using MediatR;
using Oravity.Core.Modules.Survey.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Modules.Survey.Application.Commands;

public record CreateComplaintCommand(
    long CompanyId,
    long BranchId,
    long? PatientId,
    ComplaintSource Source,
    string Subject,
    string Description,
    ComplaintPriority Priority = ComplaintPriority.Normal,
    long CreatedBy = 0,
    long? SurveyResponseId = null
) : IRequest<ComplaintResponse>;

public class CreateComplaintCommandHandler
    : IRequestHandler<CreateComplaintCommand, ComplaintResponse>
{
    private readonly AppDbContext _db;

    public CreateComplaintCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ComplaintResponse> Handle(
        CreateComplaintCommand request,
        CancellationToken cancellationToken)
    {
        var complaint = Complaint.Create(
            companyId:        request.CompanyId,
            branchId:         request.BranchId,
            createdBy:        request.CreatedBy,
            source:           request.Source,
            subject:          request.Subject,
            description:      request.Description,
            priority:         request.Priority,
            patientId:        request.PatientId,
            surveyResponseId: request.SurveyResponseId);

        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync(cancellationToken);

        return SurveyMappings.ToResponse(complaint);
    }
}
