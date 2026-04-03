using FluentAssertions;
using Oravity.SharedKernel.Services;
using Xunit;

namespace Oravity.UnitTests.Unit;

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new(new FormulaEngine());

    // ─── Test 1 — Base fiyat hesaplama ───────────────────────────────────
    [Fact]
    public void Calculate_WhenNoOverrideAndNoCampaign_ShouldReturnBasePrice()
    {
        var ctx = new PricingContext
        {
            BasePrice = 500m
        };

        var result = _engine.Calculate(ctx);

        result.FinalPrice.Should().Be(500m);
        result.TotalDiscount.Should().Be(0m);
        result.AppliedStrategy.Should().Be("BasePrice");
    }

    // ─── Test 2 — Kurum override fiyatı ──────────────────────────────────
    [Fact]
    public void Calculate_WhenInstitutionPriceSet_ShouldUseInstitutionPrice()
    {
        var ctx = new PricingContext
        {
            BasePrice        = 1000m,
            InstitutionPrice = 750m     // Kurum anlaşmalı fiyat
        };

        var result = _engine.Calculate(ctx);

        result.FinalPrice.Should().Be(750m);
        result.TotalDiscount.Should().Be(250m, "1000 - 750 = 250 indirim");
        result.AppliedStrategy.Should().Be("InstitutionOverride");
    }

    // ─── Test 2b — Institution override; kampanya uygulanmamalı ──────────
    [Fact]
    public void Calculate_WhenInstitutionOverride_CampaignShouldNotApply()
    {
        var ctx = new PricingContext
        {
            BasePrice            = 1000m,
            InstitutionPrice     = 800m,
            CampaignDiscountRate = 20m  // %20 kampanya VAR ama override öncelikli
        };

        var result = _engine.Calculate(ctx);

        // Override önceliği → 800; kampanya değil
        result.FinalPrice.Should().Be(800m);
        result.AppliedStrategy.Should().Be("InstitutionOverride");
    }

    // ─── Test 3 — Kampanya uygulaması ────────────────────────────────────
    [Fact]
    public void Calculate_WhenCampaignDiscount_ShouldApplyDiscount()
    {
        var ctx = new PricingContext
        {
            BasePrice            = 1000m,
            CampaignDiscountRate = 15m  // %15 indirim
        };

        var result = _engine.Calculate(ctx);

        result.FinalPrice.Should().Be(850m, "1000 - %15 = 850");
        result.TotalDiscount.Should().Be(150m);
        result.AppliedStrategy.Should().Be("CampaignDiscount");
    }

    // ─── Test 4 — Öncelik sırası doğru mu? ───────────────────────────────
    [Fact]
    public void PriorityOrder_InstitutionOverride_IsHigherThanCampaign()
    {
        // KURUM OVERRIDE > KAMPANYA > BASE
        var ctxOnlyBase = new PricingContext { BasePrice = 1000m };
        var ctxCampaign = new PricingContext { BasePrice = 1000m, CampaignDiscountRate = 10m };
        var ctxInstitution = new PricingContext
        {
            BasePrice = 1000m, InstitutionPrice = 700m, CampaignDiscountRate = 10m
        };

        var r1 = _engine.Calculate(ctxOnlyBase);
        var r2 = _engine.Calculate(ctxCampaign);
        var r3 = _engine.Calculate(ctxInstitution);

        r1.AppliedStrategy.Should().Be("BasePrice");
        r2.AppliedStrategy.Should().Be("CampaignDiscount");
        r3.AppliedStrategy.Should().Be("InstitutionOverride");

        // Override en düşük fiyat olmak zorunda değil ama öncelik sırası doğru
        r3.FinalPrice.Should().Be(700m, "Override her zaman kampanyadan önce gelir");
    }

    // ─── Test 5 — Formül tabanlı fiyat (FormulaEngine entegrasyonu) ──────
    [Fact]
    public void EvaluateFormula_WhenIsak1_ShouldApplyTdbRate()
    {
        var ctx = new PricingContext
        {
            BasePrice                = 1000m,
            InstitutionPrice         = 900m,
            IsInstitutionAgreement   = true
        };

        // ISAK=1 → TDB*0.90
        var result = _engine.EvaluateFormula("ISAK==1 ? TDB*0,90 : CARI*0,80", ctx);

        result.Should().Be(810m, "TDB=900, 900×0.90=810");
    }

    [Fact]
    public void EvaluateFormula_WhenIsak0_ShouldApplyCariRate()
    {
        var ctx = new PricingContext
        {
            BasePrice              = 1000m,
            IsInstitutionAgreement = false
        };

        var result = _engine.EvaluateFormula("ISAK==1 ? TDB*0,90 : CARI*0,80", ctx);

        result.Should().Be(800m, "CARI=1000, 1000×0.80=800");
    }

    // ─── Edge case — Negatif base ─────────────────────────────────────────
    [Fact]
    public void Calculate_WhenNegativeBasePrice_ShouldThrowArgumentException()
    {
        var ctx = new PricingContext { BasePrice = -1m };

        var act = () => _engine.Calculate(ctx);

        act.Should().Throw<ArgumentException>().WithMessage("*negatif*");
    }
}
