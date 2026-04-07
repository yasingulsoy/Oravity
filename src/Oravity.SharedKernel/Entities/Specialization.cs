namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hekim uzmanlık alanı (Ortodonti, Endodonti vb.)
/// Şirkete bağlı değil, global lookup tablo.
/// </summary>
public class Specialization
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Specialization() { }

    public static Specialization Create(string name, string code, int sortOrder = 0) => new()
    {
        Name = name,
        Code = code.ToUpperInvariant(),
        SortOrder = sortOrder,
        IsActive = true
    };

    public void Update(string name, int sortOrder) { Name = name; SortOrder = sortOrder; }
    public void SetActive(bool value) => IsActive = value;
}
