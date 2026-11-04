using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id);

public class GetEmployeeByIdHandler(PayClaimDbContext db)
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery query, CancellationToken ct)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == query.Id)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                Number = e.Number,
                FullName = e.FullName,
                Email = e.Email,
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
            .FirstOrDefaultAsync(ct);

        if (employee == null)
        {
            throw new NotFoundException(
                $"Employee with ID '{query.Id}' was not found.");
        }

        return employee;
    }
}