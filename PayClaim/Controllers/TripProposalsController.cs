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
using System.Security.Claims;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/trip-proposals")]
public class TripProposalsController(
    CreateTripProposalHandler createHandler, GetTripProposalsHandler getListHandler, GetTripProposalByIdHandler getDetailHandler,
    ApproveTripProposalHandler approveHandler, UpdateTripProposalHandler updateHandler, CompleteTripProposalHandler completeHandler) : ControllerBase
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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateTripProposalDto request, CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        FallbackParseParticipants(request);

        var result = await createHandler.Handle(
            new CreateTripProposalCommand(request, employeeId.Value), ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<TripProposalDetailDto>.Created(
                result, "Trip proposal submitted successfully."));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TripProposalListDto>>> GetAll(
        [FromQuery] TripProposalFilterQuery filter,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();
        var roles = GetLoggedInRoles();

        bool isAdmin = roles.Contains(
            "Admin", StringComparer.OrdinalIgnoreCase);

        if (!isAdmin &&
            (!employeeId.HasValue || employeeId.Value == Guid.Empty))
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await getListHandler.Handle(
            new GetTripProposalsQuery(
                filter,
                employeeId,
                roles),
            ct);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripProposalDetailDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();
        var roles = GetLoggedInRoles();

        bool isAdmin = roles.Contains(
            "Admin", StringComparer.OrdinalIgnoreCase);

        if (!isAdmin &&
            (!employeeId.HasValue || employeeId.Value == Guid.Empty))
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await getDetailHandler.Handle(
            new GetTripProposalByIdQuery(
                id,
                employeeId,
                roles),
            ct);

        return Ok(result);
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
     [FromRoute] Guid id,
     [FromBody] TripApprovalActionDto request,
     CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        request.Decision = TripApprovalDecision.Approved;

        var result = await approveHandler.Handle(
            new ApproveTripProposalCommand(
                id,
                request,
                employeeId.Value,
                GetLoggedInRoles()),
            ct);

        return Ok(ApiResponse<TripApprovalHistoryDto>.Ok(
            result, "Trip proposal approval step processed successfully."));
    }

    [Authorize(Roles = "Manager,Finance,Finance Manager")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] TripApprovalActionDto request,
        CancellationToken ct)
    {
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        if (request.Decision != TripApprovalDecision.NeedsRevision)
        {
            request.Decision = TripApprovalDecision.Rejected;
        }

        var result = await approveHandler.Handle(
            new ApproveTripProposalCommand(
                id,
                request,
                employeeId.Value,
                GetLoggedInRoles()),
            ct);

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
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        FallbackParseParticipants(request);

        var result = await updateHandler.Handle(
            new UpdateTripProposalCommand(
                id,
                request,
                employeeId.Value),
            ct);

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
        var employeeId = GetLoggedInEmployeeId();

        if (!employeeId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail(
                "Invalid or missing EmployeeId in token.",
                StatusCodes.Status401Unauthorized));
        }

        var result = await completeHandler.Handle(
            new CompleteTripProposalCommand(
                id,
                employeeId.Value,
                request.ReportFile,
                request.ReportNotes),
            ct);

        return Ok(ApiResponse<TripProposalDetailDto>.Ok(
            result,
            "Travel report submitted and trip marked as Completed successfully."));
    }
}
