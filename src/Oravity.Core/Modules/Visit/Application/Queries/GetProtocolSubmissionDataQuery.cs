using MediatR;
using Microsoft.EntityFrameworkCore;
using Oravity.Infrastructure.Database;
using Oravity.SharedKernel.Entities;
using Oravity.SharedKernel.Exceptions;
using Oravity.SharedKernel.Interfaces;

namespace Oravity.Core.Modules.Visit.Application.Queries;

// ─── DTO'lar ──────────────────────────────────────────────────────────────────

/// <summary>
/// Protokol bazlı e-Nabız / HBYS gönderim paketi.
/// Tanılar + tamamlanan işlemler + SUT kodları bir arada döner.
/// </summary>
public record ProtocolSubmissionData(
    string   ProtocolNo,
    DateOnly ProtocolDate,
    string   ProtocolTypeName,
    string   BranchName,
    string   PatientFullName,
    /// <summary>Şifresi çözülmüş TC Kimlik No (gönderimde kullanılır).</summary>
    string?  PatientTcNo,
    DateOnly? PatientBirthDate,
    string?  PatientGender,
    IReadOnlyList<SubmissionDiagnosis> Diagnoses,
    IReadOnlyList<SubmissionProcedure> Procedures,
    /// <summary>true = tüm işlemlerin SUT eşleşmesi var ve en az 1 tanı var.</summary>
    bool     IsReadyToSubmit,
    /// <summary>SUT kodu eksik tedaviler — entegrasyon öncesi tamamlanmalı.</summary>
    IReadOnlyList<MissingMapping> MissingMappings
);

public record SubmissionDiagnosis(
    string  IcdCode,
    string  Description,
    bool    IsPrimary,
    string? Note
);

public record SubmissionProcedure(
    string    TreatmentName,
    string?   TreatmentCode,
    /// <summary>TreatmentMapping üzerinden bulunan SUT kodu. Null = eşleşme yok.</summary>
    string?   SutCode,
    /// <summary>Hangi referans listesinden geldiği (örn. "SUT_2024").</summary>
    string?   SutListCode,
    string?   ToothNumber,
    string?   ToothSurfaces,
    DateTime? CompletedAt,
    string?   DoctorName
);

public record MissingMapping(
    string? TreatmentCode,
    string  TreatmentName
);

// ─── Query ────────────────────────────────────────────────────────────────────

public record GetProtocolSubmissionDataQuery(Guid ProtocolPublicId)
    : IRequest<ProtocolSubmissionData>;

public class GetProtocolSubmissionDataQueryHandler
    : IRequestHandler<GetProtocolSubmissionDataQuery, ProtocolSubmissionData>
{
    private readonly AppDbContext       _db;
    private readonly IEncryptionService _encryption;

    public GetProtocolSubmissionDataQueryHandler(AppDbContext db, IEncryptionService encryption)
    {
        _db         = db;
        _encryption = encryption;
    }

    public async Task<ProtocolSubmissionData> Handle(
        GetProtocolSubmissionDataQuery request,
        CancellationToken cancellationToken)
    {
        // ── Protokol + hasta + şube ──────────────────────────────────────────
        var protocol = await _db.Protocols
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProtocolPublicId, cancellationToken)
            ?? throw new NotFoundException("Protokol bulunamadı.");

        // ── TC No şifresini çöz ──────────────────────────────────────────────
        string? tcNo = null;
        if (!string.IsNullOrEmpty(protocol.Patient.TcNumberEncrypted))
        {
            try { tcNo = _encryption.Decrypt(protocol.Patient.TcNumberEncrypted); }
            catch { /* şifre çözülemezse null bırak */ }
        }

        // ── ICD tanıları (JSON snapshot'tan) ────────────────────────────────
        var diagnoses = protocol.GetIcdDiagnoses()
            .Select(e => new SubmissionDiagnosis(e.Code, e.Description, e.IsPrimary, null))
            .ToList();

        // ── Tamamlanan tedavi kalemleri (protocol → plans → items) ──────────
        // Treatment.Mappings navigation property yok; LINQ join kullanıyoruz.
        var completedRaw = await (
            from i in _db.TreatmentPlanItems.AsNoTracking()
            join plan in _db.TreatmentPlans.AsNoTracking() on i.PlanId equals plan.Id
            join t in _db.Treatments.AsNoTracking() on i.TreatmentId equals t.Id into tj
            from t in tj.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on i.DoctorId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            where plan.ProtocolId == protocol.Id
               && i.Status == TreatmentItemStatus.Completed
               && !i.IsDeleted
            orderby i.CompletedAt
            select new
            {
                i.ToothNumber,
                i.ToothSurfaces,
                i.CompletedAt,
                TreatmentId   = (long?)t.Id,
                TreatmentName = t != null ? t.Name : "Bilinmeyen",
                TreatmentCode = t != null ? t.Code : (string?)null,
                DoctorName    = u != null ? u.FullName : (string?)null,
            }
        ).ToListAsync(cancellationToken);

        // ── SUT eşleştirmeleri toplu çek ────────────────────────────────────
        var treatmentIds = completedRaw
            .Where(r => r.TreatmentId.HasValue)
            .Select(r => r.TreatmentId!.Value)
            .Distinct()
            .ToList();

        var sutLookup = treatmentIds.Count > 0
            ? await (
                from m in _db.TreatmentMappings.AsNoTracking()
                join rl in _db.ReferencePriceLists.AsNoTracking() on m.ReferenceListId equals rl.Id
                where treatmentIds.Contains(m.InternalTreatmentId)
                   && rl.Code.StartsWith("SUT")
                select new { m.InternalTreatmentId, SutCode = m.ReferenceCode, ListCode = rl.Code }
              ).ToListAsync(cancellationToken)
            : [];

        // Tedavi başına ilk SUT kaydını al
        var sutByTreatment = sutLookup
            .GroupBy(m => m.InternalTreatmentId)
            .ToDictionary(g => g.Key, g => g.First());

        // ── Procedure + missing listesi ──────────────────────────────────────
        var procedures = new List<SubmissionProcedure>();
        var missing    = new List<MissingMapping>();

        foreach (var item in completedRaw)
        {
            sutByTreatment.TryGetValue(item.TreatmentId ?? 0, out var sut);

            if (sut is null && item.TreatmentId.HasValue)
                missing.Add(new MissingMapping(item.TreatmentCode, item.TreatmentName));

            procedures.Add(new SubmissionProcedure(
                TreatmentName: item.TreatmentName,
                TreatmentCode: item.TreatmentCode,
                SutCode:       sut?.SutCode,
                SutListCode:   sut?.ListCode,
                ToothNumber:   item.ToothNumber,
                ToothSurfaces: item.ToothSurfaces,
                CompletedAt:   item.CompletedAt,
                DoctorName:    item.DoctorName?.Trim()
            ));
        }

        var isReady = missing.Count == 0 && diagnoses.Count > 0 && procedures.Count > 0;

        return new ProtocolSubmissionData(
            ProtocolNo:       protocol.ProtocolNo,
            ProtocolDate:     DateOnly.FromDateTime(protocol.CreatedAt.ToLocalTime()),
            ProtocolTypeName: protocol.ProtocolType switch
            {
                ProtocolType.Examination  => "Muayene",
                ProtocolType.Treatment    => "Tedavi",
                ProtocolType.Consultation => "Konsültasyon",
                ProtocolType.FollowUp     => "Kontrol",
                ProtocolType.Emergency    => "Acil",
                _                         => protocol.ProtocolType.ToString()
            },
            BranchName:        protocol.Branch.Name,
            PatientFullName:   $"{protocol.Patient.FirstName} {protocol.Patient.LastName}".Trim(),
            PatientTcNo:       tcNo,
            PatientBirthDate:  protocol.Patient.BirthDate,
            PatientGender:     protocol.Patient.Gender,
            Diagnoses:         diagnoses,
            Procedures:        procedures,
            IsReadyToSubmit:   isReady,
            MissingMappings:   missing
        );
    }
}
