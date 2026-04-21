using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Dijital onam formu şablonu.
/// Şirket bazlı — her kliniğin kendi form şablonları vardır.
/// </summary>
public class ConsentFormTemplate : BaseEntity
{
    public long CompanyId { get; private set; }

    /// <summary>Benzersiz form kodu (ör: GENERAL_CONSENT, IMPLANT_CONSENT).</summary>
    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    /// <summary>Dil kodu (ör: "TR", "EN").</summary>
    public string Language { get; private set; } = "TR";

    public string Version { get; private set; } = "1.0";

    /// <summary>Form metni — placeholder'lar (#hastaadi#, #tarih# vb.) içerebilir.</summary>
    public string ContentHtml { get; private set; } = default!;

    /// <summary>JSON: [{id, label, isRequired}]</summary>
    public string CheckboxesJson { get; private set; } = "[]";

    /// <summary>true → tüm tedaviler için geçerli; false → TreatmentCategoryIdsJson'a göre.</summary>
    public bool AppliesToAllTreatments { get; private set; } = true;

    /// <summary>JSON: category publicId listesi (AppliesToAllTreatments=false ise kullanılır).</summary>
    public string? TreatmentCategoryIdsJson { get; private set; }

    public bool ShowDentalChart { get; private set; } = true;
    public bool ShowTreatmentTable { get; private set; } = true;
    public bool RequireDoctorSignature { get; private set; } = false;

    public bool IsActive { get; private set; } = true;

    public long? CreatedByUserId { get; private set; }

    private ConsentFormTemplate() { }

    public static ConsentFormTemplate Create(
        long companyId,
        string code,
        string name,
        string language,
        string version,
        string contentHtml,
        string checkboxesJson,
        bool appliesToAllTreatments,
        string? treatmentCategoryIdsJson,
        bool showDentalChart,
        bool showTreatmentTable,
        bool requireDoctorSignature,
        long? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Form kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Form adı boş olamaz.", nameof(name));
        if (string.IsNullOrWhiteSpace(contentHtml))
            throw new ArgumentException("Form içeriği boş olamaz.", nameof(contentHtml));

        return new ConsentFormTemplate
        {
            CompanyId                = companyId,
            Code                     = code.Trim().ToUpperInvariant(),
            Name                     = name.Trim(),
            Language                 = language.Trim().ToUpperInvariant(),
            Version                  = version.Trim(),
            ContentHtml              = contentHtml,
            CheckboxesJson           = checkboxesJson,
            AppliesToAllTreatments   = appliesToAllTreatments,
            TreatmentCategoryIdsJson = treatmentCategoryIdsJson,
            ShowDentalChart          = showDentalChart,
            ShowTreatmentTable       = showTreatmentTable,
            RequireDoctorSignature   = requireDoctorSignature,
            IsActive                 = true,
            CreatedByUserId          = createdByUserId,
        };
    }

    public void Update(
        string name,
        string language,
        string version,
        string contentHtml,
        string checkboxesJson,
        bool appliesToAllTreatments,
        string? treatmentCategoryIdsJson,
        bool showDentalChart,
        bool showTreatmentTable,
        bool requireDoctorSignature)
    {
        Name                     = name.Trim();
        Language                 = language.Trim().ToUpperInvariant();
        Version                  = version.Trim();
        ContentHtml              = contentHtml;
        CheckboxesJson           = checkboxesJson;
        AppliesToAllTreatments   = appliesToAllTreatments;
        TreatmentCategoryIdsJson = treatmentCategoryIdsJson;
        ShowDentalChart          = showDentalChart;
        ShowTreatmentTable       = showTreatmentTable;
        RequireDoctorSignature   = requireDoctorSignature;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkUpdated();
    }
}
