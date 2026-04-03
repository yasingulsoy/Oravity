using FluentAssertions;
using Oravity.SharedKernel.Services;
using Xunit;

namespace Oravity.UnitTests.Unit;

public class FormulaEngineTests
{
    private readonly FormulaEngine _engine = new();

    // ─── Test 1a — Ternary: ISAK=1 → TDB*0.90 ───────────────────────────
    [Fact]
    public void Ternary_WhenIsak1_ShouldReturnTdbTimes090()
    {
        // ISAK==1 ? TDB*0,90 : CARI*0,80
        var vars = new Dictionary<string, decimal>
        {
            ["ISAK"] = 1m,
            ["TDB"]  = 1000m,
            ["CARI"] = 2000m
        };

        var result = _engine.Evaluate("ISAK==1 ? TDB*0,90 : CARI*0,80", vars);

        result.Should().Be(900m, "ISAK=1 → TDB(1000) × 0.90 = 900");
    }

    // ─── Test 1b — Ternary: ISAK=0 → CARI*0.80 ──────────────────────────
    [Fact]
    public void Ternary_WhenIsak0_ShouldReturnCariTimes080()
    {
        var vars = new Dictionary<string, decimal>
        {
            ["ISAK"] = 0m,
            ["TDB"]  = 1000m,
            ["CARI"] = 2000m
        };

        var result = _engine.Evaluate("ISAK==1 ? TDB*0,90 : CARI*0,80", vars);

        result.Should().Be(1600m, "ISAK=0 → CARI(2000) × 0.80 = 1600");
    }

    // ─── Test 2 — Basit çarpma: CARI*1,10 ───────────────────────────────
    [Fact]
    public void Multiply_WhenCari1000_ShouldReturn1100()
    {
        var vars = new Dictionary<string, decimal> { ["CARI"] = 1000m };

        var result = _engine.Evaluate("CARI*1,10", vars);

        result.Should().Be(1100m);
    }

    // ─── Test 3 — İç içe ternary ─────────────────────────────────────────
    [Fact]
    public void NestedTernary_ShouldEvaluateCorrectly()
    {
        // ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0,80
        // Senaryo A: ISAK=1, TDB=500, CARI=1000 → TDB(500) çünkü TDB < CARI → CARI değil TDB
        var varsA = new Dictionary<string, decimal>
        {
            ["ISAK"] = 1m, ["TDB"] = 500m, ["CARI"] = 1000m
        };
        var resultA = _engine.Evaluate("ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0,80", varsA);
        resultA.Should().Be(500m, "TDB(500) > CARI(1000) yanlış, TDB döner");

        // Senaryo B: ISAK=1, TDB=1500, CARI=1000 → TDB>CARI doğru → CARI döner
        var varsB = new Dictionary<string, decimal>
        {
            ["ISAK"] = 1m, ["TDB"] = 1500m, ["CARI"] = 1000m
        };
        var resultB = _engine.Evaluate("ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0,80", varsB);
        resultB.Should().Be(1000m, "TDB(1500) > CARI(1000) doğru → CARI döner");

        // Senaryo C: ISAK=0 → CARI*0.80
        var varsC = new Dictionary<string, decimal>
        {
            ["ISAK"] = 0m, ["TDB"] = 1500m, ["CARI"] = 1000m
        };
        var resultC = _engine.Evaluate("ISAK==1 ? (TDB>CARI ? CARI : TDB) : CARI*0,80", varsC);
        resultC.Should().Be(800m, "ISAK=0 → CARI(1000) × 0.80 = 800");
    }

    // ─── Test 4 — Hatalı formül → exception fırlatmalı ──────────────────
    [Fact]
    public void InvalidFormula_ShouldThrowFormulaException()
    {
        var vars = new Dictionary<string, decimal> { ["CARI"] = 100m };

        // Kapanmamış parantez
        var act = () => _engine.Evaluate("(CARI*2", vars);

        act.Should().Throw<FormulaException>();
    }

    // ─── Test 4b — Beklenmeyen karakter ──────────────────────────────────
    [Fact]
    public void InvalidFormula_UnexpectedChar_ShouldThrowFormulaException()
    {
        var vars = new Dictionary<string, decimal> { ["CARI"] = 100m };

        var act = () => _engine.Evaluate("CARI @ 2", vars);

        act.Should().Throw<FormulaException>();
    }

    // ─── Test 5 — Bilinmeyen değişken → exception fırlatmalı ─────────────
    [Fact]
    public void UnknownVariable_ShouldThrowUnknownVariableException()
    {
        var vars = new Dictionary<string, decimal> { ["CARI"] = 100m };

        var act = () => _engine.Evaluate("BILINMEYEN * 2", vars);

        act.Should()
            .Throw<UnknownVariableException>()
            .WithMessage("*BILINMEYEN*");
    }

    // ─── Bonus: Temel aritmetik ───────────────────────────────────────────
    [Theory]
    [InlineData("2 + 3",   5)]
    [InlineData("10 - 4",  6)]
    [InlineData("3 * 4",  12)]
    [InlineData("15 / 3",  5)]
    public void BasicArithmetic_ShouldReturnCorrectResult(string formula, decimal expected)
    {
        var result = _engine.Evaluate(formula, []);

        result.Should().Be(expected);
    }

    // ─── Bonus: Karşılaştırma operatörleri ───────────────────────────────
    [Theory]
    [InlineData("5==5", 1)]
    [InlineData("5==6", 0)]
    [InlineData("5!=6", 1)]
    [InlineData("5>3",  1)]
    [InlineData("3>5",  0)]
    [InlineData("3>=3", 1)]
    [InlineData("3<=4", 1)]
    public void ComparisonOperators_ShouldReturnBooleanDecimal(string formula, decimal expected)
    {
        var result = _engine.Evaluate(formula, []);

        result.Should().Be(expected);
    }
}
