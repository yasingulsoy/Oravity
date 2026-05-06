using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Dış laboratuvara gönderilen iş emri (SPEC §415 Laboratuvar İş Akışı).
/// Durum: pending → sent → in_progress → ready → received → fitted → completed → approved
/// Yan dallar: rejected, cancelled.
/// </summary>
public class LaboratoryWork : AuditableEntity
{
    public long CompanyId { get; private set; }
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    /// <summary>LAB-2026-000123 formatında benzersiz iş numarası (şirket içi).</summary>
    public string WorkNo { get; private set; } = default!;

    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public long DoctorId { get; private set; }
    public User Doctor { get; private set; } = default!;

    public long LaboratoryId { get; private set; }
    public Laboratory Laboratory { get; private set; } = default!;

    public long? TreatmentPlanItemId { get; private set; }
    public TreatmentPlanItem? TreatmentPlanItem { get; private set; }

    /// <summary>'prosthetic' | 'orthodontic' | 'implant' | 'other'</summary>
    public string WorkType { get; private set; } = "prosthetic";

    /// <summary>'conventional' | 'digital'</summary>
    public string DeliveryType { get; private set; } = "conventional";

    public string? ToothNumbers { get; private set; }
    public string? ShadeColor { get; private set; }

    public string Status { get; private set; } = LaboratoryWorkStatus.Pending;

    public DateTime? SentToLabAt { get; private set; }
    public DateOnly? EstimatedDeliveryDate { get; private set; }
    public DateTime? ReceivedFromLabAt { get; private set; }
    public DateTime? FittedToPatientAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public long? ApprovedByUserId { get; private set; }

    public decimal? TotalCost { get; private set; }
    public string? Currency { get; private set; }

    /// <summary>JSONB — kalemlerin özet dökümü (ad/miktar/birim fiyat/toplam).</summary>
    public string? CostDetails { get; private set; }

    public string? DoctorNotes { get; private set; }
    public string? LabNotes { get; private set; }
    public string? ApprovalNotes { get; private set; }

    /// <summary>JSONB — dosya referansları: [{ key, fileName, sizeBytes, contentType }].</summary>
    public string? Attachments { get; private set; }

    private readonly List<LaboratoryWorkItem> _items = [];
    public IReadOnlyCollection<LaboratoryWorkItem> Items => _items.AsReadOnly();

    private readonly List<LaboratoryWorkHistory> _history = [];
    public IReadOnlyCollection<LaboratoryWorkHistory> History => _history.AsReadOnly();

    private LaboratoryWork() { }

    public static LaboratoryWork Create(
        long companyId,
        long branchId,
        string workNo,
        long patientId,
        long doctorId,
        long laboratoryId,
        long? treatmentPlanItemId,
        string workType,
        string deliveryType,
        string? toothNumbers,
        string? shadeColor,
        string? doctorNotes)
    {
        if (string.IsNullOrWhiteSpace(workNo))
            throw new ArgumentException("İş numarası boş olamaz.", nameof(workNo));

        return new LaboratoryWork
        {
            CompanyId           = companyId,
            BranchId            = branchId,
            WorkNo              = workNo.Trim().ToUpperInvariant(),
            PatientId           = patientId,
            DoctorId            = doctorId,
            LaboratoryId        = laboratoryId,
            TreatmentPlanItemId = treatmentPlanItemId,
            WorkType            = string.IsNullOrWhiteSpace(workType) ? "prosthetic" : workType.Trim().ToLowerInvariant(),
            DeliveryType        = string.IsNullOrWhiteSpace(deliveryType) ? "conventional" : deliveryType.Trim().ToLowerInvariant(),
            ToothNumbers        = toothNumbers?.Trim(),
            ShadeColor          = shadeColor?.Trim(),
            DoctorNotes         = doctorNotes,
            Status              = LaboratoryWorkStatus.Pending,
        };
    }

    public void AddItem(LaboratoryWorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
    }

    public void SetCostSummary(decimal total, string currency, string? costDetailsJson)
    {
        TotalCost   = total;
        Currency    = currency;
        CostDetails = costDetailsJson;
        MarkUpdated();
    }

    public void ReplaceItems(IEnumerable<LaboratoryWorkItem> newItems)
    {
        _items.Clear();
        _items.AddRange(newItems);
        MarkUpdated();
    }

    public void UpdateMetadata(
        string workType,
        string deliveryType,
        string? toothNumbers,
        string? shadeColor,
        string? doctorNotes,
        long? treatmentPlanItemId)
    {
        if (Status != LaboratoryWorkStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen (pending) iş emirleri güncellenebilir.");

        WorkType            = string.IsNullOrWhiteSpace(workType) ? "prosthetic" : workType.Trim().ToLowerInvariant();
        DeliveryType        = string.IsNullOrWhiteSpace(deliveryType) ? "conventional" : deliveryType.Trim().ToLowerInvariant();
        ToothNumbers        = toothNumbers?.Trim();
        ShadeColor          = shadeColor?.Trim();
        DoctorNotes         = doctorNotes;
        TreatmentPlanItemId = treatmentPlanItemId;
        MarkUpdated();
    }

    public void SetAttachments(string? attachmentsJson)
    {
        Attachments = attachmentsJson;
        MarkUpdated();
    }

    // ─── State machine ────────────────────────────────────────────────────
    public void SendToLab(int estimatedDays, long userId, string? notes = null)
    {
        EnsureStatus(LaboratoryWorkStatus.Pending);
        Status      = LaboratoryWorkStatus.Sent;
        SentToLabAt = DateTime.UtcNow;
        if (estimatedDays > 0)
        {
            EstimatedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(estimatedDays));
        }
        AppendHistory(LaboratoryWorkStatus.Pending, LaboratoryWorkStatus.Sent, userId, notes ?? "Laboratuvara gönderildi");
        MarkUpdated();
    }

    public void MarkInProgress(long userId, string? notes = null)
    {
        if (Status != LaboratoryWorkStatus.Sent)
            throw new InvalidOperationException("Sadece gönderilmiş işler 'laboratuvarda' olarak işaretlenebilir.");
        Status = LaboratoryWorkStatus.InProgress;
        AppendHistory(LaboratoryWorkStatus.Sent, LaboratoryWorkStatus.InProgress, userId, notes ?? "Laboratuvarda yapılıyor");
        MarkUpdated();
    }

    public void MarkReady(long userId, string? notes = null)
    {
        if (Status != LaboratoryWorkStatus.Sent && Status != LaboratoryWorkStatus.InProgress)
            throw new InvalidOperationException("Sadece gönderilmiş veya devam eden işler 'hazır' olarak işaretlenebilir.");
        var prev = Status;
        Status = LaboratoryWorkStatus.Ready;
        AppendHistory(prev, LaboratoryWorkStatus.Ready, userId, notes ?? "Lab'da hazır");
        MarkUpdated();
    }

    public void Receive(long userId, string? labNotes = null)
    {
        if (Status is not (LaboratoryWorkStatus.Sent or LaboratoryWorkStatus.InProgress or LaboratoryWorkStatus.Ready))
            throw new InvalidOperationException("Geçersiz durum geçişi: lab'dan alma.");

        var prev = Status;
        Status            = LaboratoryWorkStatus.Received;
        ReceivedFromLabAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(labNotes)) LabNotes = labNotes;
        AppendHistory(prev, LaboratoryWorkStatus.Received, userId, "Lab'dan alındı");
        MarkUpdated();
    }

    public void Fit(long userId, string? note = null)
    {
        EnsureStatus(LaboratoryWorkStatus.Received);
        Status            = LaboratoryWorkStatus.Fitted;
        FittedToPatientAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(note))
            DoctorNotes = string.IsNullOrWhiteSpace(DoctorNotes) ? note : $"{DoctorNotes}\n{note}";
        AppendHistory(LaboratoryWorkStatus.Received, LaboratoryWorkStatus.Fitted, userId, note ?? "Hastaya takıldı");
        MarkUpdated();
    }

    public void Complete(long userId, string? note = null)
    {
        EnsureStatus(LaboratoryWorkStatus.Fitted);
        Status      = LaboratoryWorkStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AppendHistory(LaboratoryWorkStatus.Fitted, LaboratoryWorkStatus.Completed, userId, note ?? "İşlem tamamlandı");
        MarkUpdated();
    }

    public void Approve(long userId, string? notes)
    {
        EnsureStatus(LaboratoryWorkStatus.Completed);
        Status           = LaboratoryWorkStatus.Approved;
        ApprovedAt       = DateTime.UtcNow;
        ApprovedByUserId = userId;
        ApprovalNotes    = notes;
        AppendHistory(LaboratoryWorkStatus.Completed, LaboratoryWorkStatus.Approved, userId, notes ?? "Yönetici onayladı");
        MarkUpdated();
    }

    public void Reject(long userId, string rejectionReason)
    {
        EnsureStatus(LaboratoryWorkStatus.Completed);
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Red nedeni zorunludur.", nameof(rejectionReason));
        Status        = LaboratoryWorkStatus.Rejected;
        ApprovalNotes = rejectionReason;
        AppendHistory(LaboratoryWorkStatus.Completed, LaboratoryWorkStatus.Rejected, userId, rejectionReason);
        MarkUpdated();
    }

    /// <summary>
    /// Ara adımları atlayarak doğrudan 'completed' durumuna geçer.
    /// Komut katmanında <c>laboratory.work_approve</c> yetkisi kontrol edilir.
    /// </summary>
    public void FastComplete(long userId, string? notes = null)
    {
        if (Status is LaboratoryWorkStatus.Completed
                    or LaboratoryWorkStatus.Approved
                    or LaboratoryWorkStatus.Rejected
                    or LaboratoryWorkStatus.Cancelled)
            throw new InvalidOperationException($"'{Status}' durumundaki iş emri hızlı tamamlanamaz.");

        var prev = Status;
        Status      = LaboratoryWorkStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AppendHistory(prev, LaboratoryWorkStatus.Completed, userId, notes ?? "Hızlı tamamlama (ara adımlar atlandı)");
        MarkUpdated();
    }

    public void Cancel(long userId, string? reason)
    {
        if (Status is LaboratoryWorkStatus.Approved or LaboratoryWorkStatus.Completed or LaboratoryWorkStatus.Cancelled)
            throw new InvalidOperationException("Tamamlanmış veya onaylanmış iş iptal edilemez.");
        var prev = Status;
        Status = LaboratoryWorkStatus.Cancelled;
        AppendHistory(prev, LaboratoryWorkStatus.Cancelled, userId, reason ?? "İptal edildi");
        MarkUpdated();
    }

    private void EnsureStatus(string expected)
    {
        if (Status != expected)
            throw new InvalidOperationException(
                $"Bu işlem yalnızca '{expected}' durumundaki iş için yapılabilir (şu anki: '{Status}').");
    }

    private void AppendHistory(string from, string to, long userId, string? notes)
    {
        _history.Add(LaboratoryWorkHistory.Create(Id, from, to, userId, notes));
    }
}

/// <summary>Laboratuvar iş emri durum sabitleri.</summary>
public static class LaboratoryWorkStatus
{
    public const string Pending    = "pending";
    public const string Sent       = "sent";
    public const string InProgress = "in_progress";
    public const string Ready      = "ready";
    public const string Received   = "received";
    public const string Fitted     = "fitted";
    public const string Completed  = "completed";
    public const string Approved   = "approved";
    public const string Rejected   = "rejected";
    public const string Cancelled  = "cancelled";

    public static readonly string[] All =
    [
        Pending, Sent, InProgress, Ready, Received, Fitted, Completed, Approved, Rejected, Cancelled
    ];
}
