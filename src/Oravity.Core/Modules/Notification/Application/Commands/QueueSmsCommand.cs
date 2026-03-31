using MediatR;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Notification.Application.Commands;

public record QueueSmsCommand(
    string ToPhone,
    string Message,
    string SourceType,
    int ProviderId = 1
) : IRequest<long>;

public class QueueSmsCommandHandler : IRequestHandler<QueueSmsCommand, long>
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QueueSmsCommandHandler(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<long> Handle(QueueSmsCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToPhone))
            throw new ArgumentException("Telefon numarası boş olamaz.");

        var companyId = _tenant.CompanyId
            ?? throw new ForbiddenException("SMS kuyruğa eklemek için şirket bağlamı gereklidir.");

        var sms = SmsQueue.Create(
            companyId:  companyId,
            providerId: request.ProviderId,
            toPhone:    request.ToPhone,
            message:    request.Message,
            sourceType: request.SourceType);

        _db.SmsQueues.Add(sms);
        await _db.SaveChangesAsync(cancellationToken);

        return sms.Id;
    }
}
