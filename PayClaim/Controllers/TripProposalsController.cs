using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;
using PayClaim.Features.TripProposals.Commands.ApproveTripProposal;
using PayClaim.Features.TripProposals.Commands.CreateTripProposal;
using PayClaim.Features.TripProposals.Queries.GetTripProposalById;
using PayClaim.Features.TripProposals.Queries.GetTripProposals;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/trip-proposals")]
public class TripProposalsController(
    CreateTripProposalHandler createHandler,
    GetTripProposalsHandler getListHandler,
    GetTripProposalByIdHandler getDetailHandler,
    ApproveTripProposalHandler approveHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTripProposalDto request, CancellationToken ct)
    {
        if ((request.Participants == null || request.Participants.Count == 0) && Request.HasFormContentType)
        {
            // 1. Fallback jika Participants dikirim sebagai string JSON di dalam FormData (umum di React / client API)
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

            // 2. Fallback jika Swagger UI mengirimkan field peserta secara flat (employeeId, isPic, amount)
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

        var result = await createHandler.Handle(new CreateTripProposalCommand(request), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TripProposalDetailDto>.Created(result, "Trip proposal submitted successfully."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TripProposalFilterQuery filter, CancellationToken ct)
    {
        var result = await getListHandler.Handle(new GetTripProposalsQuery(filter), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await getDetailHandler.Handle(new GetTripProposalByIdQuery(id), ct);
        return Ok(ApiResponse<TripProposalDetailDto>.Ok(result, "Trip proposal detail retrieved successfully."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid id, [FromBody] TripApprovalActionDto request, CancellationToken ct)
    {
        request.Decision = ApprovalDecision.Approved;
        var result = await approveHandler.Handle(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(result, "Trip proposal approved successfully."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] TripApprovalActionDto request, CancellationToken ct)
    {
        request.Decision = ApprovalDecision.Rejected;
        var result = await approveHandler.Handle(new ApproveTripProposalCommand(id, request), ct);
        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(result, "Trip proposal rejected successfully."));
    }
}
