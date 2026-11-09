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
using PayClaim.Features.Claims.Commands.UpdateClaim;
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
    PayClaimHandler payHandler,
    UpdateClaimHandler updateHandler) : ControllerBase
{
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

    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateClaimDto request, CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await createHandler.Handle(new CreateClaimCommand(request, employeeId.Value), ct);

        return StatusCode(StatusCodes.Status201Created, ApiResponse<ClaimDetailDto>.Created(result, "Claim submitted successfully."));
    }

    [Authorize(Roles = "Admin,Manager,Finance,Finance Manager")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetAll([FromQuery] ClaimFilterQuery filter,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();
        var roles = GetLoggedInRoles();

        var result = await getListHandler.Handle(
            new GetClaimsQuery(filter, employeeId,
                roles),
            ct);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("my-claims")]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetMyClaims(
        [FromQuery] ClaimFilterQuery filter,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("Invalid or missing EmployeeId in token.", StatusCodes.Status401Unauthorized));
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
    /// Mengambil detail lengkap klaim berdasarkan ID beserta riwayat approval-nya.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> GetById(
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

        return Ok(result);
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        var approverId = GetLoggedInEmployeeId();

        if (!approverId.HasValue)
        {
            return Unauthorized(
                ApiResponse.Fail("Invalid or missing EmployeeId in token.", StatusCodes.Status401Unauthorized));
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

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
       [FromRoute] Guid id,
       [FromBody] ClaimApprovalActionDto request,
       CancellationToken ct)
    {
        var approverId = GetLoggedInEmployeeId();

        if (!approverId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var roles = GetLoggedInRoles();

        if (request.Decision != ApprovalDecision.NeedsRevision)
        {
            request.Decision = ApprovalDecision.Rejected;
        }

        var result = await rejectHandler.Handle(
            new RejectClaimCommand(
                id,
                request,
                approverId.Value,
                roles),
            ct);

        return Ok(
            ApiResponse<ClaimApprovalHistoryDto>.Ok(
                result,
                "Claim rejection / revision recorded successfully."));
    }

    [Authorize(Roles = "Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(
        [FromRoute] Guid id,
        [FromBody] ClaimApprovalActionDto? request,
        CancellationToken ct)
    {
        var payerId = GetLoggedInEmployeeId();

        if (!payerId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await payHandler.Handle(
            new PayClaimCommand(
                id,
                request?.Notes,
                payerId.Value),
            ct);

        return Ok(
            ApiResponse<ClaimApprovalHistoryDto>.Ok(
                result,
                "Claim marked as Paid and added to payroll successfully."));
    }

    /// <summary>
    /// Mengedit & mengajukan ulang (Resubmit) klaim yang berstatus NeedsRevision atau Submitted.
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromForm] CreateClaimDto request, CancellationToken ct)
    {
        var submitterId = GetLoggedInEmployeeId();
        if (!submitterId.HasValue)
        {
            var claimVal = User.FindFirst("employeeId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            submitterId = Guid.TryParse(claimVal, out var parsedId) ? parsedId : null;
        }

        var result = await updateHandler.Handle(new UpdateClaimCommand(id, request, submitterId), ct);
        return Ok(ApiResponse<ClaimDetailDto>.Ok(result, "Claim resubmitted successfully."));
    }
}
