using Oravity.SharedKernel.BaseEntities;

namespace Oravity.SharedKernel.Entities;

/// <summary>Ülke — ISO 3166-1 alpha-2 kodu ile</summary>
public class Country : BaseEntity
{
    public string Name { get; private set; } = default!;
    /// <summary>ISO alpha-2: TR, US, DE …</summary>
    public string IsoCode { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<City> Cities { get; private set; } = [];

    private Country() { }

    public static Country Create(string name, string isoCode, int sortOrder = 0)
        => new() { Name = name, IsoCode = isoCode.ToUpperInvariant(), SortOrder = sortOrder, IsActive = true };
}

/// <summary>İl / Şehir</summary>
public class City : BaseEntity
{
    public long CountryId { get; private set; }
    public Country Country { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<District> Districts { get; private set; } = [];

    private City() { }

    public static City Create(long countryId, string name, int sortOrder = 0)
        => new() { CountryId = countryId, Name = name, SortOrder = sortOrder, IsActive = true };
}

/// <summary>İlçe</summary>
public class District : BaseEntity
{
    public long CityId { get; private set; }
    public City City { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private District() { }

    public static District Create(long cityId, string name, int sortOrder = 0)
        => new() { CityId = cityId, Name = name, SortOrder = sortOrder, IsActive = true };
}

/// <summary>Uyruk / Milliyet</summary>
public class Nationality : BaseEntity
{
    public string Name { get; private set; } = default!;
    /// <summary>ISO alpha-2 ülke kodu (TC vatandaşlığı için "TC")</summary>
    public string Code { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Nationality() { }

    public static Nationality Create(string name, string code, int sortOrder = 0)
        => new() { Name = name, Code = code.ToUpperInvariant(), SortOrder = sortOrder, IsActive = true };

    public void Update(string name, int sortOrder, bool isActive)
    {
        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
        MarkUpdated();
    }
}
