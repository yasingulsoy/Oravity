namespace Oravity.SharedKernel.Entities;

/// <summary>
/// Hakediş şablonu iş başı hesaplama için tedavi bazında özel fiyat/yüzde tanımı.
/// JobStartCalculation = CustomPrices olduğunda kullanılır.
/// </summary>
public class TemplateJobStartPrice
{
    public long Id { get; private set; }

    public long TemplateId { get; private set; }
    public DoctorCommissionTemplate Template { get; private set; } = default!;

    public long TreatmentId { get; private set; }
    public Treatment Treatment { get; private set; } = default!;

    public JobStartPriceType PriceType { get; private set; }
    /// <summary>Sabit ücret ise tutar, yüzde ise % değeri.</summary>
    public decimal Value { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private TemplateJobStartPrice() { }

    public static TemplateJobStartPrice Create(
        long templateId,
        long treatmentId,
        JobStartPriceType priceType,
        decimal value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (priceType == JobStartPriceType.Percentage && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Yüzde 0–100 arası olmalı.");

        return new TemplateJobStartPrice
        {
            TemplateId  = templateId,
            TreatmentId = treatmentId,
            PriceType   = priceType,
            Value       = value,
            CreatedAt   = DateTime.UtcNow
        };
    }

    public void Update(JobStartPriceType priceType, decimal value)
    {
        PriceType = priceType;
        Value     = value;
    }
}
