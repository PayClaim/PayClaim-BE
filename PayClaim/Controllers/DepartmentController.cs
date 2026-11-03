using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Master;
using PayClaim.Features.Master.Departments.Commands.CreateDepartment;
using PayClaim.Features.Master.Departments.Queries.GetDepartments;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController(
    CreateDepartmentHandler createDepartmentHandler, GetDepartmentsHandler getDepartmentsHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var departments = await getDepartmentsHandler.Handle(
            new GetDepartmentsQuery(), ct);

        return Ok(departments);
    }

    [HttpPost]
    public async Task<IActionResult> Create( [FromBody] CreateDepartmentDto request, CancellationToken ct)
    {
        await createDepartmentHandler.Handle(
            new CreateDepartmentCommand(request), ct);

        return Ok("Department created successfully.");
    }
}