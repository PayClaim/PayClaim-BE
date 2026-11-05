using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.Master;
using PayClaim.Features.Employees.Commands.CreateEmployee;
using PayClaim.Features.Employees.Commands.UpdateEmployee;
using PayClaim.Features.Employees.Queries;
using PayClaim.Features.Employees.Queries.GetEmployeeById;
using PayClaim.Features.Employees.Queries.GetEmployees;
using PayClaim.Features.Master.Departments.Commands.CreateDepartment;

namespace PayClaim.Features.Employees;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(GetEmployeesHandler getEmployeesHandler, GetEmployeeByIdHandler getEmployeeByIdHandler, 
    CreateEmployeeHandler createEmployeeHandler, UpdateEmployeeHandler updateEmployeeHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await getEmployeesHandler.Handle( new GetEmployeesQuery(),ct);

        return Ok(ApiResponse<List<EmployeeDto>>
            .Ok( result, "Employees retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await getEmployeeByIdHandler.Handle(new GetEmployeeByIdQuery(id), ct);

        return Ok(ApiResponse<EmployeeDto>
            .Ok(result, "Employees retrieved successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto request, CancellationToken ct)
    {
        var employeeId = await createEmployeeHandler.Handle(new CreateEmployeeCommand(request), ct);

        return CreatedAtAction(nameof(GetById), new { id = employeeId }, new { id = employeeId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
    Guid id,
    [FromBody] UpdateEmployeeDto request,
    CancellationToken ct)
    {
        await updateEmployeeHandler.Handle(
            new UpdateEmployeeCommand(id, request), ct);

        return NoContent();
    }
}