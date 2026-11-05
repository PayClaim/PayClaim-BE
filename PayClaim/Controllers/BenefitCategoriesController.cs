using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.BenefitCategories.Commands.CreateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.UpdateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.DeleteBenefitCategory;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategories;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategoryById;
using Microsoft.AspNetCore.Authorization;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitCategoriesController(CreateBenefitCategoryHandler createHandler, GetBenefitCategoriesHandler getAllHandler, GetBenefitCategoryByIdQueryHandler getByIdHandler,
    UpdateBenefitCategoryHandler updateHandler, DeleteBenefitCategoryHandler deleteHandler) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<BenefitCategoryDto>>> GetAll(CancellationToken ct)
    {
        var categories = await getAllHandler.Handle(
            new GetBenefitCategoriesQuery(), ct);

        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BenefitCategoryDto>> GetById(
        int id,
        CancellationToken ct)
    {
        var category = await getByIdHandler.Handle(
            new GetBenefitCategoryByIdQuery(id), ct);

        return Ok(category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBenefitCategoryDto request,
        CancellationToken ct)
    {
        var id = await createHandler.Handle(
            new CreateBenefitCategoryCommand(request), ct);

        return Created($"/api/benefitcategories/{id}", new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CreateBenefitCategoryDto request,
        CancellationToken ct)
    {
        await updateHandler.Handle(
            new UpdateBenefitCategoryCommand(id, request), ct);

        return Ok("Benefit category updated successfully.");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        await deleteHandler.Handle(
            new DeleteBenefitCategoryCommand(id), ct);

        return Ok("Benefit category deleted successfully.");
    }
}
