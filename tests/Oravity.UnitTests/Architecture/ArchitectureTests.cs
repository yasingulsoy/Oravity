using System.Reflection;
using MediatR;
using NetArchTest.Rules;
using Oravity.Core.Controllers;
using Xunit;

namespace Oravity.UnitTests.Architecture;

/// <summary>
/// NetArchTest tabanlı mimari kural testleri.
/// CI/CD'de ihlal varsa build fail olur (SPEC §MİMARİ KURAL ENFORCEMENT).
/// </summary>
public class ArchitectureTests
{
    // ─── Assembly referansları ────────────────────────────────────────────
    private static readonly Assembly CoreAssembly
        = typeof(AuthController).Assembly;                  // Oravity.Core

    private static readonly Assembly BackendAssembly
        = typeof(Oravity.Backend.Controllers.HealthController).Assembly; // Oravity.Backend

    private static readonly Assembly SharedKernelAssembly
        = typeof(Oravity.SharedKernel.Services.FormulaEngine).Assembly;  // Oravity.SharedKernel

    // ─── Test 1 — Core modüller Backend'e bağımlı olamaz ────────────────
    [Fact]
    public void CoreAssembly_ShouldNotDependOn_BackendAssembly()
    {
        var result = Types
            .InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOnAny("Oravity.Backend")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Core modülünden Backend'e bağımlılık tespit edildi:\n" +
            string.Join("\n", result.FailingTypeNames ?? []));
    }

    // ─── Test 2 — Domain katmanı Infrastructure'a bağımlı olamaz ────────
    [Fact]
    public void DomainTypes_ShouldNotDependOn_Infrastructure()
    {
        // Core assembly içindeki *.Domain.* namespace'leri
        var result = Types
            .InAssembly(CoreAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain.")
            .Should()
            .NotHaveDependencyOnAny(
                "Oravity.Infrastructure",
                "Oravity.Core.Infrastructure",   // guard: modül-içi infra namespace
                "Microsoft.EntityFrameworkCore",  // EF Core — domain bilmemeli
                "Hangfire")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Domain katmanında Infrastructure bağımlılığı tespit edildi:\n" +
            string.Join("\n", result.FailingTypeNames ?? []));
    }

    // ─── Test 3 — Controller'lar Infrastructure'a doğrudan bağımlı olamaz
    [Fact]
    public void Controllers_ShouldNotDependOn_Infrastructure()
    {
        var result = Types
            .InAssembly(CoreAssembly)
            .That()
            .ResideInNamespace("Oravity.Core.Controllers")
            .Should()
            .NotHaveDependencyOnAny(
                "Oravity.Infrastructure",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Controller(lar) Infrastructure'a doğrudan bağımlı:\n" +
            string.Join("\n", result.FailingTypeNames ?? []));
    }

    // ─── Test 4 — Handler'lar IRequestHandler implement etmeli ──────────
    // MediatR'da iki geçerli form var:
    //   IRequestHandler<TRequest, TResponse>  (yanıt döner)
    //   IRequestHandler<TRequest>              (Unit döner — void benzeri)
    [Fact]
    public void CommandHandlers_ShouldImplement_IRequestHandler()
    {
        var handlerTypes = Types
            .InAssembly(CoreAssembly)
            .That()
            .ResideInNamespaceContaining(".Application.Commands")
            .And()
            .HaveNameEndingWith("Handler")
            .GetTypes();

        if (!handlerTypes.Any()) return;

        var twoGeneric = typeof(IRequestHandler<,>);
        var oneGeneric = typeof(IRequestHandler<>);

        var violators = handlerTypes
            .Where(t => !t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                          (i.GetGenericTypeDefinition() == twoGeneric ||
                           i.GetGenericTypeDefinition() == oneGeneric)))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(
            !violators.Any(),
            $"Şu handler(lar) IRequestHandler implement etmiyor:\n" +
            string.Join("\n", violators));
    }

    // ─── Test 5 — SharedKernel sadece kendine bağımlı olmalı ────────────
    [Fact]
    public void SharedKernel_ShouldNotDependOn_CoreOrBackend()
    {
        var result = Types
            .InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny("Oravity.Core", "Oravity.Backend", "Oravity.Infrastructure")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"SharedKernel bağımlılık ihlali:\n" +
            string.Join("\n", result.FailingTypeNames ?? []));
    }
}
