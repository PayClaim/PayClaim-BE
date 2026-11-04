using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.BenefitCategories.Commands.CreateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.UpdateBenefitCategory;
using PayClaim.Features.BenefitCategories.Commands.DeleteBenefitCategory;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategories;
using PayClaim.Features.BenefitCategories.Queries.GetBenefitCategoryById;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BenefitCategoriesController(CreateBenefitCategoryHandler createHandler, GetBenefitCategoriesHandler getAllHandler, GetBenefitCategoryByIdQueryHandler getByIdHandler,
    UpdateBenefitCategoryHandler updateHandler, DeleteBenefitCategoryHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await getAllHandler.Handle(
            new GetBenefitCategoriesQuery(), ct);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var category = await getByIdHandler.Handle(
            new GetBenefitCategoryByIdQuery(id), ct);

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBenefitCategoryDto request,
        CancellationToken ct)
    {
        var id = await createHandler.Handle(
            new CreateBenefitCategoryCommand(request), ct);

        return Created($"/api/benefitcategories/{id}", new { id });
    }

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