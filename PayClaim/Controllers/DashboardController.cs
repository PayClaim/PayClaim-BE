using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Dashboard;
using PayClaim.Features.Dashboard.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var result = await sender.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }
}
