using MediatR;
using Microsoft.AspNetCore.Authorization;
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
public class DepartmentsController(ISender sender) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DepartmentDto>>> GetAll(CancellationToken ct)
    {
        var departments = await sender.Send(new GetDepartmentsQuery(), ct);
        return Ok(departments);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(string id, CancellationToken ct)
    {
        var department = await sender.Send(new GetDepartmentByIdQuery(id), ct);
        return Ok(department);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto request, CancellationToken ct)
    {
        await sender.Send(new CreateDepartmentCommand(request), ct);
        return Ok(new { message = "Department created successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDepartmentDto request, CancellationToken ct)
    {
        await sender.Send(new UpdateDepartmentCommand(id, request), ct);
        return Ok(new { message = "Department updated successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await sender.Send(new DeleteDepartmentCommand(id), ct);
        return Ok(new { message = "Department deleted successfully." });
    }
}
