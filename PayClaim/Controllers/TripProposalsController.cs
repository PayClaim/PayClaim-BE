using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;
using PayClaim.Features.TripProposals.Commands.ApproveTripProposal;
using PayClaim.Features.TripProposals.Commands.CompleteTripProposal;
using PayClaim.Features.TripProposals.Commands.CreateTripProposal;
using PayClaim.Features.TripProposals.Commands.UpdateTripProposal;
using PayClaim.Features.TripProposals.Queries.GetTripProposalById;
using PayClaim.Features.TripProposals.Queries.GetTripProposals;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/trip-proposals")]
public class TripProposalsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTripProposalDto request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateTripProposalCommand(request), ct);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<TripProposalDetailDto>.Created(result, "Trip proposal submitted successfully."));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TripProposalListDto>>> GetAll(
        [FromQuery] TripProposalFilterQuery filter,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetTripProposalsQuery(filter), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripProposalDetailDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetTripProposalByIdQuery(id), ct);
        return Ok(result);
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] TripApprovalActionDto request,
        CancellationToken ct)
    {
        request.Decision = TripApprovalDecision.Approved;
        var result = await sender.Send(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(
            result, "Trip proposal approval step processed successfully."));
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager,Admin")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] TripApprovalActionDto request,
        CancellationToken ct)
    {
        if (request.Decision != TripApprovalDecision.NeedsRevision)
        {
            request.Decision = TripApprovalDecision.Rejected;
        }

        var result = await sender.Send(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(
            result, "Trip proposal decision recorded successfully."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromForm] CreateTripProposalDto request,
        CancellationToken ct)
    {
        var result = await sender.Send(new UpdateTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripProposalDetailDto>.Ok(
            result, "Trip proposal resubmitted successfully."));
    }

    [Authorize]
    [HttpPost("{id:guid}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        [FromForm] CompleteTripProposalDto request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CompleteTripProposalCommand(id, ReportFile: request.ReportFile, ReportNotes: request.ReportNotes),
            ct);

        return Ok(ApiResponse<TripProposalDetailDto>.Ok(
            result, "Travel report submitted and trip marked as Completed successfully."));
    }
}
