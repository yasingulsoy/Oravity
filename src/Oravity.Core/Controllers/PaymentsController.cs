using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oravity.Core.Filters;
using Oravity.Core.Modules.Finance.Application;
using Oravity.Core.Modules.Finance.Application.Commands;
using Oravity.Core.Modules.Finance.Application.Queries;
using Oravity.SharedKernel.Entities;

namespace Oravity.Core.Controllers;

/// <summary>
/// Finans modülü — ödeme, dağıtım, hakediş ve raporlama.
/// Tüm endpoint'ler JWT + permission koruması altındadır.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Bakiye ────────────────────────────────────────────────────────────

    /// <summary>Hastanın güncel cari bakiyesini döner (borç/alacak).</summary>
    [HttpGet("api/patients/{patientId:long}/balance")]
    [RequirePermission("payment:view")]
    [ProducesResponseType(typeof(PatientBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBalance(long patientId)
    {
        var result = await _mediator.Send(new GetPatientBalanceQuery(patientId));
        return Ok(result);
    }

    /// <summary>
    /// Hasta cari hesap özeti — tedavi kalemleri, ödemeler, dağıtımlar
    /// ve bakiyeyi birlikte döner.
    /// </summary>
    [HttpGet("api/patients/{patientId:long}/account")]
    [RequirePermission("payment:view")]
    [ProducesResponseType(typeof(PatientAccountSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(long patientId)
    {
        var result = await _mediator.Send(new GetPatientAccountQuery(patientId));
        return Ok(result);
    }

    // ── Ödeme ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ödeme al + tamamlanan kalemlere otomatik FIFO dağıtım (tek işlem).
    /// Klinik kasasının standart "ödeme al" akışı.
    /// </summary>
    [HttpPost("api/patients/{patientId:long}/collect-payment")]
    [RequirePermission("payment:create")]
    [ProducesResponseType(typeof(CollectPaymentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CollectPayment(
        long patientId, [FromBody] CollectPaymentRequest request)
    {
        var result = await _mediator.Send(new CollectPaymentCommand(
            patientId,
            request.Amount,
            request.Method,
            request.PaymentDate,
            request.Currency     ?? "TRY",
            request.ExchangeRate,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Yeni ödeme kaydı oluşturur.
    /// Başarıda outbox'a PaymentReceived event'i eklenir.
    /// </summary>
    [HttpPost("api/payments")]
    [RequirePermission("payment:create")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var result = await _mediator.Send(new CreatePaymentCommand(
            request.PatientId,
            request.Amount,
            request.Method,
            request.PaymentDate,
            request.Currency ?? "TRY",
            request.ExchangeRate,
            request.Notes));

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Ödemeyi tedavi kalemlerine otomatik dağıtır.</summary>
    [HttpPost("api/payments/{id:guid}/allocate")]
    [RequirePermission("payment:create")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentAllocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Allocate(Guid id, [FromBody] AllocatePaymentRequest request)
    {
        var allocations = request.Allocations
            .Select(a => new AllocationItem(a.TreatmentPlanItemId, a.Amount))
            .ToList();

        var result = await _mediator.Send(new AllocatePaymentCommand(
            id, allocations, AllocationMethod.Automatic, request.Notes));
        return Ok(result);
    }

    /// <summary>
    /// Manuel dağıtım talebi oluşturur. Yetkili onayı bekler.
    /// İzin: allocation:request
    /// </summary>
    [HttpPost("api/allocations/request")]
    [RequirePermission("allocation:request")]
    [ProducesResponseType(typeof(AllocationApprovalResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestManual([FromBody] RequestManualAllocationRequest request)
    {
        var result = await _mediator.Send(new RequestManualAllocationCommand(
            request.PaymentPublicId, request.TreatmentPlanItemId,
            request.Amount, request.Notes, request.Source));
        return Created($"api/allocations/approvals/{result.PublicId}", result);
    }

    /// <summary>Bekleyen manuel dağıtım taleplerini listeler.</summary>
    [HttpGet("api/allocations/approvals")]
    [RequirePermission("allocation:view")]
    [ProducesResponseType(typeof(IReadOnlyList<AllocationApprovalResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovals(
        [FromQuery] AllocationApprovalStatus? status = null,
        [FromQuery] long? patientId = null)
    {
        var result = await _mediator.Send(new GetAllocationApprovalsQuery(status, patientId));
        return Ok(result);
    }

    /// <summary>Manuel dağıtım talebini onaylar — allocation kaydı oluşur.</summary>
    [HttpPost("api/allocations/approvals/{id:guid}/approve")]
    [RequirePermission("allocation:approve")]
    [ProducesResponseType(typeof(AllocationApprovalResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveAllocation(Guid id, [FromBody] ApproveAllocationRequest? request)
    {
        var result = await _mediator.Send(new ApproveAllocationCommand(id, request?.Notes));
        return Ok(result);
    }

    /// <summary>Manuel dağıtım talebini reddeder.</summary>
    [HttpPost("api/allocations/approvals/{id:guid}/reject")]
    [RequirePermission("allocation:approve")]
    [ProducesResponseType(typeof(AllocationApprovalResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectAllocation(Guid id, [FromBody] RejectAllocationRequest request)
    {
        var result = await _mediator.Send(new RejectAllocationCommand(id, request.Reason));
        return Ok(result);
    }

    /// <summary>
    /// Ödeme iadesi yapar. Tüm allocation'lar MarkRefunded olur.
    /// İzin: payment:refund
    /// </summary>
    [HttpPost("api/payments/{id:guid}/refund")]
    [RequirePermission("payment:refund")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest request)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id, request.Reason));
        return Ok(result);
    }

    // ── Hakediş ───────────────────────────────────────────────────────────

    /// <summary>
    /// Hekim hakediş listesi — tarih aralığı ve durum filtresi ile.
    /// İzin: commission:view
    /// </summary>
    [HttpGet("api/commissions")]
    [RequirePermission("commission:view")]
    [ProducesResponseType(typeof(PagedCommissionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] long? doctorId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] CommissionStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _mediator.Send(
            new GetDoctorCommissionsQuery(doctorId, from, to, status, page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Tamamlanmış tedavi için hakediş hesapla ve dağıt.
    /// İzin: commission:distribute
    /// </summary>
    [HttpPost("api/commissions/distribute")]
    [RequirePermission("commission:distribute")]
    [ProducesResponseType(typeof(DoctorCommissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Distribute([FromBody] DistributeCommissionRequest request)
    {
        var result = await _mediator.Send(
            new DistributeCommissionCommand(request.TreatmentPlanItemId, request.CommissionRate));
        return Ok(result);
    }

    // ── Raporlar ──────────────────────────────────────────────────────────

    /// <summary>
    /// Günlük kasa raporu — ödeme yöntemi bazında toplam.
    /// İzin: report:view_daily
    /// </summary>
    [HttpGet("api/reports/daily")]
    [RequirePermission("report:view_daily")]
    [ProducesResponseType(typeof(DailyReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DailyReport(
        [FromQuery] DateOnly date,
        [FromQuery] long? branchId = null)
    {
        var result = await _mediator.Send(new GetDailyReportQuery(date, branchId));
        return Ok(result);
    }
}

// ─── Request DTO'lar ───────────────────────────────────────────────────────

public record CollectPaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    DateOnly PaymentDate,
    string? Currency     = null,
    decimal ExchangeRate = 1m,
    string? Notes        = null
);

public record CreatePaymentRequest(
    long PatientId,
    decimal Amount,
    PaymentMethod Method,
    DateOnly PaymentDate,
    string? Currency,
    decimal ExchangeRate = 1m,
    string? Notes = null
);

public record AllocationItemRequest(long TreatmentPlanItemId, decimal Amount);

public record AllocatePaymentRequest(IReadOnlyList<AllocationItemRequest> Allocations, string? Notes = null);

public record RequestManualAllocationRequest(
    Guid PaymentPublicId,
    long TreatmentPlanItemId,
    decimal Amount,
    string? Notes,
    AllocationSource Source = AllocationSource.Patient
);

public record ApproveAllocationRequest(string? Notes);
public record RejectAllocationRequest(string Reason);

public record RefundPaymentRequest(string? Reason);

public record DistributeCommissionRequest(long TreatmentPlanItemId, decimal CommissionRate);
