using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Master.Departments.Queries.GetDepartmentById;

public record GetDepartmentByIdQuery(string Id) : IRequest<DepartmentDto>;

public class GetDepartmentByIdQueryHandler(PayClaimDbContext db) : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery query, CancellationToken ct)
    {
        var department = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == query.Id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                ParentDeptId = d.ParentDepartmentId,
                ParentDeptName = d.ParentDepartment != null
                    ? d.ParentDepartment.Name
                    : null
            })
            .FirstOrDefaultAsync(ct);

        if (department == null)
        {
            throw new NotFoundException("Department not found.");
        }

        return department;
    }
}
