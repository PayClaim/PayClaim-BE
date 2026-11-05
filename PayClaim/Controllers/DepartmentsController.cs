using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Departments.Commands.UpdataDepartment;
using PayClaim.Features.Departments.Queries.GetDepartment;
using PayClaim.Features.Master.Departments.Commands.CreateDepartment;
using PayClaim.Features.Master.Departments.Commands.DeleteDepartment;
using PayClaim.Features.Master.Departments.Queries.GetDepartmentById;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController(
    CreateDepartmentHandler createDepartmentHandler, GetDepartmentsHandler getDepartmentsHandler, 
    GetDepartmentByIdQueryHandler getDepartmentByIdHandler, UpdateDepartmentHandler updateDepartmentHandler,
    DeleteDepartmentHandler deleteDepartmentHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentDto>>> GetAll(CancellationToken ct)
    {
        var departments = await getDepartmentsHandler.Handle(
            new GetDepartmentsQuery(), ct);

        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(string id, CancellationToken ct)
    {
        var department = await getDepartmentByIdHandler.Handle(
            new GetDepartmentByIdQuery(id), ct);

        return Ok(department);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto request, CancellationToken ct)
    {
        await createDepartmentHandler.Handle(
            new CreateDepartmentCommand(request), ct);

        return Ok("Department created successfully.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDepartmentDto request, CancellationToken ct)
    {
        await updateDepartmentHandler.Handle(
            new UpdateDepartmentCommand(id, request), ct);

        return Ok("Department created successfully.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await deleteDepartmentHandler.Handle(
            new DeleteDepartmentCommand(id), ct);

        return Ok("Department deleted successfully.");
    }
}
