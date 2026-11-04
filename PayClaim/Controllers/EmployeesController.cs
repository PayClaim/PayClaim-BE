using Microsoft.AspNetCore.Mvc;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.Master;
using PayClaim.Features.Employees.Queries.GetEmployeeById;
using PayClaim.Features.Employees.Queries;
using PayClaim.Features.Employees.Queries.GetEmployeeById;
using PayClaim.Features.Employees.Queries.GetEmployees;

namespace PayClaim.Features.Employees;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(GetEmployeesHandler getEmployeesHandler, GetEmployeeByIdHandler getEmployeeByIdHandler) : ControllerBase
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
}