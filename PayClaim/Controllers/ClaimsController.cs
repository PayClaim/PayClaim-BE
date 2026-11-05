using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;
using PayClaim.Entities;
using PayClaim.Enums;
using PayClaim.Features.Claims.Commands.ApproveClaim;
using PayClaim.Features.Claims.Commands.CreateClaim;
using PayClaim.Features.Claims.Commands.PayClaim;
using PayClaim.Features.Claims.Commands.RejectClaim;
using PayClaim.Features.Claims.Queries.GetClaimById;
using PayClaim.Features.Claims.Queries.GetClaims;
using System.Security.Claims;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController(
    CreateClaimHandler createHandler,
    GetClaimsHandler getListHandler,
    GetClaimByIdHandler getDetailHandler,
    ApproveClaimHandler approveHandler,
    RejectClaimHandler rejectHandler,
    PayClaimHandler payHandler) : ControllerBase
{
    /// <summary>
    /// Mengajukan klaim / reimbursement baru (mendukung lampiran bukti nota/kuitansi via multipart/form-data).
    /// </summary>
    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateClaimDto request, CancellationToken ct)
    {
        // 1. Ambil EmployeeId dari JWT
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        // 2. Kirim EmployeeId dari JWT ke Handler
        var result = await createHandler.Handle(new CreateClaimCommand(request, employeeId), ct);

        return StatusCode( StatusCodes.Status201Created,ApiResponse<ClaimDetailDto>.Created(result,"Claim submitted successfully."));
    }

    /// <summary>
    /// Mengambil riwayat klaim sesuai kewenangan Admin, Manager, atau Finance.
    /// </summary>
    [Authorize(Roles = "Admin,Manager,Finance")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetAll(
        [FromQuery] ClaimFilterQuery filter,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();
        var roles = GetLoggedInRoles();

        var result = await getListHandler.Handle(
            new GetClaimsQuery(
                filter,
                employeeId,
                roles),
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Mengambil riwayat Claim milik user yang sedang login.
    /// </summary>
    [Authorize]
    [HttpGet("my-claims")]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetMyClaims(
        [FromQuery] ClaimFilterQuery filter,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(
                ApiResponse.Fail(
                    "Invalid or missing EmployeeId in token.",
                    StatusCodes.Status401Unauthorized));
        }

        var roles = GetLoggedInRoles();

        var result = await getListHandler.Handle(
            new GetClaimsQuery(
                filter,
                employeeId,
                roles,
                IsMyClaims: true),
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail Claim jika user memiliki kewenangan.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClaimDetailDto>>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();
        var roles = GetLoggedInRoles();

        var result = await getDetailHandler.Handle(
            new GetClaimByIdQuery(
                id,
                employeeId,
                roles),
            ct);

        return Ok(
            ApiResponse<ClaimDetailDto>.Ok(
                result,
                "Claim detail retrieved successfully."));
    }

    /// <summary>
    /// Menyetujui Claim sesuai tahap Manager atau Finance.
    /// </summary>
    [Authorize(Roles = "Manager,Finance")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] ClaimApprovalActionDto request,
        CancellationToken ct)
    {
        var approverId = GetLoggedInEmployeeId();

        if (!approverId.HasValue)
        {
            return Unauthorized(
                ApiResponse.Fail(
                    "Invalid or missing EmployeeId in token.",
                    StatusCodes.Status401Unauthorized));
        }

        var roles = GetLoggedInRoles();

        request.Decision = ApprovalDecision.Approved;

        var result = await approveHandler.Handle(
            new ApproveClaimCommand(
                id,
                request,
                approverId.Value,
                roles),
            ct);

        return Ok(
            ApiResponse<ClaimApprovalHistoryDto>.Ok(
                result,
                "Claim approval step processed successfully."));
    }

    /// <summary>
    /// Menolak atau meminta revisi klaim di tingkat Manager (1) maupun Finance (2).
    /// </summary>
    [Authorize(Roles = "Manager, Finance")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        if (request.Decision != ApprovalDecision.NeedsRevision)
        {
            request.Decision = ApprovalDecision.Rejected;
        }

        var result = await rejectHandler.Handle(new RejectClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim rejection / revision recorded successfully."));
    }

    /// <summary>
    /// Finalisasi Pencairan Dana ke Payroll / Transfer (Status berubah dari Approved ke Paid/Enum 6).
    /// </summary>
    [Authorize(Roles = "Finance")]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto? request, CancellationToken ct)
    {
        var result = await payHandler.Handle(new PayClaimCommand(id, request?.Notes, request?.ApproverId), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim marked as Paid and added to payroll successfully."));
    }
    private Guid? GetLoggedInEmployeeId()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

        if (Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return employeeId;
        }

        return null;
    }

    private string[] GetLoggedInRoles()
    {
        return User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
