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
public class TripProposalsController(
    CreateTripProposalHandler createHandler,
    GetTripProposalsHandler getListHandler,
    GetTripProposalByIdHandler getDetailHandler,
    ApproveTripProposalHandler approveHandler,
    UpdateTripProposalHandler updateHandler,
    CompleteTripProposalHandler completeHandler) : ControllerBase
{
    private void FallbackParseParticipants(CreateTripProposalDto request)
    {
        if ((request.Participants == null || request.Participants.Count == 0) && Request.HasFormContentType)
        {
            if (Request.Form.TryGetValue("Participants", out var jsonVal) || Request.Form.TryGetValue("participants", out jsonVal))
            {
                var str = jsonVal.ToString().Trim();
                if (str.StartsWith("[") && str.EndsWith("]"))
                {
                    try
                    {
                        var items = System.Text.Json.JsonSerializer.Deserialize<List<ParticipantItemDto>>(
                            str,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (items != null && items.Count > 0)
                        {
                            request.Participants = items;
                        }
                    }
                    catch { }
                }
            }

            if (request.Participants == null || request.Participants.Count == 0)
            {
                var empIds = Request.Form["employeeId"].Count > 0 ? Request.Form["employeeId"] : Request.Form["EmployeeId"];
                if (empIds.Count > 0)
                {
                    var isPics = Request.Form["isPic"].Count > 0 ? Request.Form["isPic"] : Request.Form["IsPic"];
                    var amounts = Request.Form["amount"].Count > 0 ? Request.Form["amount"] : Request.Form["Amount"];

                    request.Participants ??= new();
                    for (int i = 0; i < empIds.Count; i++)
                    {
                        if (Guid.TryParse(empIds[i], out var id))
                        {
                            bool isPic = i < isPics.Count && bool.TryParse(isPics[i], out var b) && b;
                            decimal amount = i < amounts.Count && decimal.TryParse(amounts[i], out var a) ? a : request.TotalAmount;
                            request.Participants.Add(new ParticipantItemDto
                            {
                                EmployeeId = id,
                                IsPic = isPic,
                                Amount = amount
                            });
                        }
                    }
                }
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTripProposalDto request, CancellationToken ct)
    {
        FallbackParseParticipants(request);
        var result = await createHandler.Handle(new CreateTripProposalCommand(request), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TripProposalDetailDto>.Created(result, "Trip proposal submitted successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TripProposalListDto>>> GetAll([FromQuery] TripProposalFilterQuery filter, CancellationToken ct)
    {
        var result = await getListHandler.Handle(new GetTripProposalsQuery(filter), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TripProposalDetailDto>>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await getDetailHandler.Handle(new GetTripProposalByIdQuery(id), ct);
        return Ok(ApiResponse<TripProposalDetailDto>.Ok(result, "Trip proposal detail retrieved successfully."));
    }

    /// <summary>
    /// Persetujuan bertingkat SPD:
    /// - Jika status Submitted (1) ➡️ Disetujui Manager, diteruskan ke Finance dengan status UnderReview (2).
    /// - Jika status UnderReview (2) ➡️ Disetujui Finance sekaligus menetapkan budget aktual, status jadi Approved (4).
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] TripApprovalActionDto request, CancellationToken ct)
    {
        request.Decision = ApprovalDecision.Approved;
        var result = await approveHandler.Handle(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(result, "Trip proposal approval step processed successfully."));
    }

    /// <summary>
    /// Menolak atau meminta revisi SPD (Manager atau Finance).
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] TripApprovalActionDto request, CancellationToken ct)
    {
        if (request.Decision != ApprovalDecision.NeedsRevision)
        {
            request.Decision = ApprovalDecision.Rejected;
        }

        var result = await approveHandler.Handle(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(result, "Trip proposal decision recorded successfully."));
    }

    /// <summary>
    /// Mengedit & mengajukan ulang (Resubmit) SPD yang berstatus NeedsRevision atau Submitted.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromForm] CreateTripProposalDto request, CancellationToken ct)
    {
        FallbackParseParticipants(request);
        var result = await updateHandler.Handle(new UpdateTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripProposalDetailDto>.Ok(result, "Trip proposal resubmitted successfully."));
    }

    /// <summary>
    /// Pengumpulan Laporan Pasca-Perjalanan Dinas (Submit Travel Report) ➡️ Status berubah ke Completed (6).
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        [FromForm] CompleteTripProposalDto request,
        CancellationToken ct)
    {
        var result = await completeHandler.Handle(new CompleteTripProposalCommand(id, request.ReportFile, request.ReportNotes), ct);
        return Ok(ApiResponse<TripProposalDetailDto>.Ok(result, "Travel report submitted and trip marked as Completed successfully."));
    }
}
