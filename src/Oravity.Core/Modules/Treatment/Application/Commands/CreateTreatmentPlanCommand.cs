using System.Text.Json;
using MediatR;
using Oravity.Core.Modules.Treatment.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;
using PlanEntity = Oravity.SharedKernel.Entities.TreatmentPlan;

namespace Oravity.Core.Modules.Treatment.Application.Commands;

public record CreateTreatmentPlanCommand(
    long PatientId,
    long DoctorId,
    string Name,
    string? Notes
) : IRequest<TreatmentPlanResponse>;

public class CreateTreatmentPlanCommandHandler
    : IRequestHandler<CreateTreatmentPlanCommand, TreatmentPlanResponse>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public CreateTreatmentPlanCommandHandler(
        AppDbContext db,
        ITenantContext tenant,
        ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<TreatmentPlanResponse> Handle(
        CreateTreatmentPlanCommand request,
        CancellationToken cancellationToken)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Tedavi planı oluşturmak için şube bağlamı gereklidir.");

        var plan = PlanEntity.Create(
            patientId: request.PatientId,
            branchId:  branchId,
            doctorId:  request.DoctorId,
            name:      request.Name,
            notes:     request.Notes);

        if (_user.IsAuthenticated)
            plan.SetCreatedBy(_user.UserId, _user.TenantId);

        _db.TreatmentPlans.Add(plan);

        var payload = JsonSerializer.Serialize(new
        {
            plan.PublicId,
            plan.PatientId,
            plan.BranchId,
            plan.DoctorId,
            plan.Name
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("TreatmentPlanCreated", payload));

        await _db.SaveChangesAsync(cancellationToken);

        return TreatmentPlanMappings.ToResponse(plan, []);
    }
}
