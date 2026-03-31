using MediatR;
using Oravity.Core.Modules.Core.PatientRecord.Application;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Core.PatientRecord.Application.Commands;

/// <summary>
/// Dosya meta kaydını oluşturur. Fiziksel upload ayrı bir storage endpoint üzerinden yapılır.
/// FilePath: upload tamamlandıktan sonra dönen URL/path iletilir.
/// </summary>
public record UploadPatientFileCommand(
    long PatientId,
    PatientFileType FileType,
    string FilePath,
    string? Category = null,
    string? Title = null,
    int? FileSize = null,
    string? FileExt = null,
    string? Note = null,
    DateTime? TakenAt = null
) : IRequest<PatientFileResponse>;

public class UploadPatientFileCommandHandler
    : IRequestHandler<UploadPatientFileCommand, PatientFileResponse>
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    public UploadPatientFileCommandHandler(
        AppDbContext db, ICurrentUser user, ITenantContext tenant)
    {
        _db = db;
        _user = user;
        _tenant = tenant;
    }

    public async Task<PatientFileResponse> Handle(
        UploadPatientFileCommand request,
        CancellationToken cancellationToken)
    {
        var branchId = _tenant.BranchId
            ?? throw new ForbiddenException("Bu işlem için şube bağlamı gereklidir.");

        var file = PatientFile.Create(
            patientId:  request.PatientId,
            branchId:   branchId,
            fileType:   request.FileType,
            filePath:   request.FilePath,
            uploadedBy: _user.UserId,
            category:   request.Category,
            title:      request.Title,
            fileSize:   request.FileSize,
            fileExt:    request.FileExt,
            note:       request.Note,
            takenAt:    request.TakenAt);

        _db.PatientFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        return PatientRecordMappings.ToFileResponse(file);
    }
}
