using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.BenefitCategories.Commands.CreateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.DeleteBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.UpdateBenefitCategory;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategories;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategoryById;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitCategoriesController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<BenefitCategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await sender.Send(new GetBenefitCategoriesQuery(), ct);
        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BenefitCategoryDto>> GetById(int id, CancellationToken ct)
    {
        var category = await sender.Send(new GetBenefitCategoryByIdQuery(id), ct);
        return Ok(category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBenefitCategoryDto request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateBenefitCategoryCommand(request), ct);
        return Created($"/api/benefitcategories/{id}", new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateBenefitCategoryDto request, CancellationToken ct)
    {
        await sender.Send(new UpdateBenefitCategoryCommand(id, request), ct);
        return Ok(new { message = "Benefit category updated successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteBenefitCategoryCommand(id), ct);
        return Ok(new { message = "Benefit category deleted successfully." });
    }
}
