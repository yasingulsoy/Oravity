using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hasta kaydı. Tenant izolasyonu branch_id üzerinden sağlanır.
/// TC Kimlik No ve Pasaport No uygulama katmanında AES-256 ile şifreli, aramalarda SHA-256 hash kullanılır.
/// </summary>
public class Patient : AuditableEntity
{
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = default!;

    // ── Kimlik ─────────────────────────────────────────────────────────
    /// <summary>AES-256 şifreli TC Kimlik No</summary>
    public string? TcNumberEncrypted { get; private set; }
    /// <summary>SHA-256 hash — aramada kullanılır</summary>
    public string? TcNumberHash { get; private set; }
    /// <summary>AES-256 şifreli Pasaport No</summary>
    public string? PassportNoEncrypted { get; private set; }

    // ── Kişisel Bilgiler ───────────────────────────────────────────────
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? MotherName { get; private set; }
    public string? FatherName { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    /// <summary>male / female / other</summary>
    public string? Gender { get; private set; }
    /// <summary>Evli / Bekar / Boşanmış / Dul</summary>
    public string? MaritalStatus { get; private set; }
    public string? Nationality { get; private set; }
    public long? CitizenshipTypeId { get; private set; }
    public CitizenshipType? CitizenshipType { get; private set; }
    public string? Occupation { get; private set; }
    /// <summary>none / cigarette / pipe / hookah</summary>
    public string? SmokingType { get; private set; }
    /// <summary>0=Yok 1=Hamile 2=Emziriyor</summary>
    public int? PregnancyStatus { get; private set; }

    // ── İletişim ───────────────────────────────────────────────────────
    /// <summary>Cep telefonu</summary>
    public string? Phone { get; private set; }
    public string? HomePhone { get; private set; }
    public string? WorkPhone { get; private set; }
    public string? Email { get; private set; }

    // ── Adres ──────────────────────────────────────────────────────────
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? Address { get; private set; }

    // ── Tıbbi ──────────────────────────────────────────────────────────
    /// <summary>A+, A-, B+, B-, AB+, AB-, 0+, 0-</summary>
    public string? BloodType { get; private set; }

    // ── Geliş / Kurum ──────────────────────────────────────────────────
    public long? ReferralSourceId { get; private set; }
    public ReferralSource? ReferralSource { get; private set; }
    public string? ReferralPerson { get; private set; }
    /// <summary>Anlaşmalı Kurum (AK) — kurumsal/kamu tipi kurum.</summary>
    public long? AgreementInstitutionId { get; private set; }
    public Institution? AgreementInstitution { get; private set; }

    /// <summary>Özel Sağlık Sigortası (ÖSS) — sigorta tipi kurum.</summary>
    public long? InsuranceInstitutionId { get; private set; }
    public Institution? InsuranceInstitution { get; private set; }

    // ── Sistem / İdari ─────────────────────────────────────────────────
    public string? Notes { get; private set; }
    public string PreferredLanguageCode { get; private set; } = "tr";
    public bool SmsOptIn { get; private set; } = true;
    public bool CampaignOptIn { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    // ── Acil Durum Kişileri ────────────────────────────────────────────
    public ICollection<PatientEmergencyContact> EmergencyContacts { get; private set; } = [];

    private Patient() { }

    public static Patient Create(
        long branchId,
        string firstName,
        string lastName,
        string? phone = null,
        string? email = null,
        DateOnly? birthDate = null,
        string? gender = null,
        string? tcNumberEncrypted = null,
        string? tcNumberHash = null,
        string? address = null,
        string? bloodType = null,
        string? preferredLanguageCode = null,
        string? motherName = null,
        string? fatherName = null,
        string? nationality = null,
        string? country = null,
        string? city = null,
        string? district = null,
        string? homePhone = null,
        string? workPhone = null)
    {
        return new Patient
        {
            BranchId = branchId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MotherName = motherName?.Trim(),
            FatherName = fatherName?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.ToLowerInvariant().Trim(),
            BirthDate = birthDate,
            Gender = gender,
            TcNumberEncrypted = tcNumberEncrypted,
            TcNumberHash = tcNumberHash,
            Address = address,
            BloodType = bloodType,
            Nationality = nationality,
            Country = country ?? "Türkiye",
            City = city,
            District = district,
            HomePhone = homePhone?.Trim(),
            WorkPhone = workPhone?.Trim(),
            PreferredLanguageCode = preferredLanguageCode ?? "tr",
            IsActive = true
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? phone,
        string? email,
        DateOnly? birthDate,
        string? gender,
        string? address,
        string? bloodType,
        string? preferredLanguageCode,
        string? motherName = null,
        string? fatherName = null,
        string? maritalStatus = null,
        string? nationality = null,
        string? occupation = null,
        string? smokingType = null,
        int? pregnancyStatus = null,
        string? homePhone = null,
        string? workPhone = null,
        string? country = null,
        string? city = null,
        string? district = null,
        string? neighborhood = null,
        long? referralSourceId = null,
        string? referralPerson = null,
        long? agreementInstitutionId = null,
        long? insuranceInstitutionId = null,
        long? citizenshipTypeId = null,
        string? notes = null,
        bool? smsOptIn = null,
        bool? campaignOptIn = null)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MotherName = motherName?.Trim();
        FatherName = fatherName?.Trim();
        Phone = phone?.Trim();
        Email = email?.ToLowerInvariant().Trim();
        BirthDate = birthDate;
        Gender = gender;
        MaritalStatus = maritalStatus;
        Nationality = nationality;
        CitizenshipTypeId = citizenshipTypeId;
        Occupation = occupation;
        SmokingType = smokingType;
        PregnancyStatus = pregnancyStatus;
        HomePhone = homePhone?.Trim();
        WorkPhone = workPhone?.Trim();
        Address = address;
        Country = country;
        City = city;
        District = district;
        Neighborhood = neighborhood;
        BloodType = bloodType;
        ReferralSourceId = referralSourceId;
        ReferralPerson = referralPerson;
        AgreementInstitutionId = agreementInstitutionId;
        InsuranceInstitutionId = insuranceInstitutionId;
        Notes = notes;
        if (preferredLanguageCode is not null) PreferredLanguageCode = preferredLanguageCode;
        if (smsOptIn.HasValue) SmsOptIn = smsOptIn.Value;
        if (campaignOptIn.HasValue) CampaignOptIn = campaignOptIn.Value;
        MarkUpdated();
    }

    public void UpdateTcNumber(string? encrypted, string? hash)
    {
        TcNumberEncrypted = encrypted;
        TcNumberHash = hash;
        MarkUpdated();
    }

    public void UpdatePassport(string? encrypted)
    {
        PassportNoEncrypted = encrypted;
        MarkUpdated();
    }

    public void SetActive(bool value)
    {
        IsActive = value;
        MarkUpdated();
    }
}

// ─── Acil Durum Kişisi ────────────────────────────────────────────────────────
public class PatientEmergencyContact : BaseEntities.BaseEntity
{
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = default!;

    public int SortOrder { get; private set; }   // 1 veya 2
    public string? FullName { get; private set; }
    public string? Relationship { get; private set; }  // Anne / Baba / Eş / Kardeş
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }

    private PatientEmergencyContact() { }

    public static PatientEmergencyContact Create(
        long patientId, int sortOrder,
        string? fullName, string? relationship,
        string? phone, string? email = null, string? address = null)
        => new()
        {
            PatientId = patientId,
            SortOrder = sortOrder,
            FullName = fullName?.Trim(),
            Relationship = relationship,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim()
        };

    public void Update(string? fullName, string? relationship,
        string? phone, string? email, string? address)
    {
        FullName = fullName?.Trim();
        Relationship = relationship;
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        MarkUpdated();
    }
}

// ─── Vatandaşlık Tipi ─────────────────────────────────────────────────────────
/// <summary>
/// CompanyId = null → platform geneli varsayılan (tüm şirketler görür).
/// CompanyId = X   → yalnızca o şirkete özgü ek kayıt.
/// </summary>
public class CitizenshipType : BaseEntities.BaseEntity
{
    /// <summary>null = global default</summary>
    public long? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CitizenshipType() { }

    public static CitizenshipType Create(string name, string code, int sortOrder = 0, long? companyId = null)
        => new() { Name = name, Code = code, SortOrder = sortOrder, IsActive = true, CompanyId = companyId };

    public void Update(string name, int sortOrder, bool isActive)
    {
        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
        MarkUpdated();
    }
}

// ─── Geliş Şekli ─────────────────────────────────────────────────────────────
/// <summary>
/// CompanyId = null → platform geneli varsayılan (tüm şirketler görür).
/// CompanyId = X   → yalnızca o şirkete özgü ek kayıt.
/// </summary>
public class ReferralSource : BaseEntities.BaseEntity
{
    /// <summary>null = global default</summary>
    public long? CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ReferralSource() { }

    public static ReferralSource Create(string name, string code, int sortOrder = 0, long? companyId = null)
        => new() { Name = name, Code = code, SortOrder = sortOrder, IsActive = true, CompanyId = companyId };

    public void Update(string name, int sortOrder, bool isActive)
    {
        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
        MarkUpdated();
    }
}
