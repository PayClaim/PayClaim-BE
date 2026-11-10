using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery : IRequest<List<EmployeeDto>>;

public class GetEmployeesHandler(PayClaimDbContext db) : IRequestHandler<GetEmployeesQuery, List<EmployeeDto>>
{
    public async Task<List<EmployeeDto>> Handle(GetEmployeesQuery query, CancellationToken ct)
    {
        return await db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Number)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                Number = e.Number,
                FullName = e.FullName,
                Email = e.Email,
                HireDate = e.HireDate,
                IsActive = e.IsActive,

                PositionId = e.PositionId,
                PositionTitle = e.Position.Title,

                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department.Name,

                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null
                    ? e.Manager.FullName
                    : null
            })
            .ToListAsync(ct);
    }
}
