using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;
using PayClaim.Enums;
using PayClaim.Features.Claims.Commands.ApproveClaim;
using PayClaim.Features.Claims.Commands.CreateClaim;
using PayClaim.Features.Claims.Commands.RejectClaim;
using PayClaim.Features.Claims.Queries.GetClaimById;
using PayClaim.Features.Claims.Queries.GetClaims;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController(
    CreateClaimHandler createHandler,
    GetClaimsHandler getListHandler,
    GetClaimByIdHandler getDetailHandler,
    ApproveClaimHandler approveHandler,
    RejectClaimHandler rejectHandler) : ControllerBase
{
    /// <summary>
    /// Mengajukan klaim / reimbursement baru (mendukung lampiran bukti nota/kuitansi via multipart/form-data).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateClaimDto request, CancellationToken ct)
    {
        var result = await createHandler.Handle(new CreateClaimCommand(request), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ClaimDetailDto>.Created(result, "Claim submitted successfully."));
    }

    /// <summary>
    /// Mengambil seluruh riwayat klaim dengan filter dan pagination (Admin / Finance / Manager).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetAll([FromQuery] ClaimFilterQuery filter, CancellationToken ct)
    {
        var result = await getListHandler.Handle(new GetClaimsQuery(filter), ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil riwayat klaim khusus milik user/karyawan yang sedang login (me-reuse GetClaimsHandler).
    /// </summary>
    [HttpGet("my-claims")]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetMyClaims([FromQuery] ClaimFilterQuery filter, CancellationToken ct)
    {
        // Resolusi EmployeeId: Dari JWT Token jika terautentikasi, atau dari filter jika mode development
        var claimVal = User.FindFirst("employeeId")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(claimVal, out var loggedInEmpId))
        {
            filter.EmployeeId = loggedInEmpId;
        }

        var result = await getListHandler.Handle(new GetClaimsQuery(filter), ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail lengkap klaim berdasarkan ID beserta riwayat approval-nya.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClaimDetailDto>>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await getDetailHandler.Handle(new GetClaimByIdQuery(id), ct);
        return Ok(ApiResponse<ClaimDetailDto>.Ok(result, "Claim detail retrieved successfully."));
    }

    /// <summary>
    /// Menyetujui klaim (memotong saldo benefit jika klaim tipe Benefit).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        request.Decision = ApprovalDecision.Approved;
        var result = await approveHandler.Handle(new ApproveClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim approved successfully."));
    }

    /// <summary>
    /// Menolak atau meminta revisi klaim (saldo benefit tidak terpotong).
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        if (request.Decision != ApprovalDecision.NeedsRevision)
        {
            request.Decision = ApprovalDecision.Rejected;
        }

        var result = await rejectHandler.Handle(new RejectClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim decision recorded successfully."));
    }
}
