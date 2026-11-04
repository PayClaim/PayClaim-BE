using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.BenefitPlans.Commands;
using PayClaim.Features.BenefitPlans.Queries;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitPlansController(
    CreateBenefitPlanHandler createHandler, GetBenefitPlansHandler getAllHandler, GetBenefitPlanByIdHandler getByIdHandler,
    UpdateBenefitPlanHandler updateHandler, DeleteBenefitPlanHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getAllHandler.Handle(
            new GetBenefitPlansQuery(), ct);

        return Ok(result);
    }

    [HttpGet("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<IActionResult> GetById( string departmentId, Guid benefitId, string positionId, CancellationToken ct)
    {
        var result = await getByIdHandler.Handle(
            new GetBenefitPlanByIdQuery(
                departmentId,
                benefitId,
                positionId), ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertBenefitPlanDto request, CancellationToken ct)
    {
        await createHandler.Handle(
            new CreateBenefitPlanCommand(request), ct);

        return Ok(new { message = "Benefit plan created successfully." });
    }

    [HttpPut("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<IActionResult> Update(string departmentId, Guid benefitId, string positionId, 
        [FromBody] UpsertBenefitPlanDto request, CancellationToken ct)
    {
        await updateHandler.Handle(
            new UpdateBenefitPlanCommand(
                departmentId,
                benefitId,
                positionId,
                request), ct);

        return Ok(new { message = "Benefit plan updated successfully." });
    }

    [HttpDelete("{departmentId}/{benefitId:guid}/{positionId}")]
    public async Task<IActionResult> Delete( string departmentId, Guid benefitId, string positionId, CancellationToken ct)
    {
        await deleteHandler.Handle(
            new DeleteBenefitPlanCommand(
                departmentId,
                benefitId,
                positionId), ct);

        return Ok(new { message = "Benefit plan deleted successfully." });
    }
}