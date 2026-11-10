using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;
using PayClaim.Enums;
using PayClaim.Features.Claims.Commands.ApproveClaim;
using PayClaim.Features.Claims.Commands.CreateClaim;
using PayClaim.Features.Claims.Commands.PayClaim;
using PayClaim.Features.Claims.Commands.RejectClaim;
using PayClaim.Features.Claims.Commands.UpdateClaim;
using PayClaim.Features.Claims.Queries.GetClaimById;
using PayClaim.Features.Claims.Queries.GetClaims;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateClaimDto request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateClaimCommand(request), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ClaimDetailDto>.Created(result, "Claim submitted successfully."));
    }

    [Authorize(Roles = "Admin,Manager,Finance,Finance Manager")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetAll([FromQuery] ClaimFilterQuery filter, CancellationToken ct)
    {
        var result = await sender.Send(new GetClaimsQuery(filter), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("my-claims")]
    public async Task<ActionResult<PagedResponse<ClaimListDto>>> GetMyClaims([FromQuery] ClaimFilterQuery filter, CancellationToken ct)
    {
        var result = await sender.Send(new GetClaimsQuery(filter, IsMyClaims: true), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetClaimByIdQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        request.Decision = ApprovalDecision.Approved;
        var result = await sender.Send(new ApproveClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim approval step processed successfully."));
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto request, CancellationToken ct)
    {
        if (request.Decision != ApprovalDecision.NeedsRevision)
        {
            request.Decision = ApprovalDecision.Rejected;
        }

        var result = await sender.Send(new RejectClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim rejection / revision recorded successfully."));
    }

    [Authorize(Roles = "Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay([FromRoute] Guid id, [FromBody] ClaimApprovalActionDto? request, CancellationToken ct)
    {
        var result = await sender.Send(new PayClaimCommand(id, request?.Notes), ct);
        return Ok(ApiResponse<ClaimApprovalHistoryDto>.Ok(result, "Claim marked as Paid and added to payroll successfully."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromForm] CreateClaimDto request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateClaimCommand(id, request), ct);
        return Ok(ApiResponse<ClaimDetailDto>.Ok(result, "Claim resubmitted successfully."));
    }
}
