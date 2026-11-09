using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.Master;
using PayClaim.Features.Employees.Commands.CreateEmployee;
using PayClaim.Features.Employees.Commands.UpdateEmployee;
using PayClaim.Features.Employees.Commands.DeleteEmployee;
using PayClaim.Features.Employees.Commands.ActivateEmployee;
using PayClaim.Features.Employees.Queries;
using PayClaim.Features.Employees.Queries.GetEmployeeById;
using PayClaim.Features.Employees.Queries.GetEmployees;
using PayClaim.Features.Master.Departments.Commands.CreateDepartment;

namespace PayClaim.Features.Employees;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(
    GetEmployeesHandler getEmployeesHandler,
    GetEmployeeByIdHandler getEmployeeByIdHandler,
    CreateEmployeeHandler createEmployeeHandler,
    UpdateEmployeeHandler updateEmployeeHandler,
    DeleteEmployeeHandler deleteEmployeeHandler,
    ActivateEmployeeHandler activateEmployeeHandler) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll(CancellationToken ct)
    {
        var result = await getEmployeesHandler.Handle(new GetEmployeesQuery(), ct);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getEmployeeByIdHandler.Handle(new GetEmployeeByIdQuery(id), ct);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto request, CancellationToken ct)
    {
        var employeeId = await createEmployeeHandler.Handle(new CreateEmployeeCommand(request), ct);

        return StatusCode(StatusCodes.Status201Created, new { id = employeeId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmployeeDto request,
        CancellationToken ct)
    {
        await updateEmployeeHandler.Handle(new UpdateEmployeeCommand(id, request), ct);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await deleteEmployeeHandler.Handle(new DeleteEmployeeCommand(id), ct);

        return Ok(new { message = "Employee deactivated successfully." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await activateEmployeeHandler.Handle(new ActivateEmployeeCommand(id), ct);

        return Ok(new { message = "Employee activated successfully." });
    }
}
