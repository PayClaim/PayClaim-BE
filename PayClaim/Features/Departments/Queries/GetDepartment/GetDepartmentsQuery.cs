
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Departments.Queries.GetDepartment;

public record GetDepartmentsQuery;

public class GetDepartmentsHandler(PayClaimDbContext db)
{
    public async Task<IReadOnlyCollection<DepartmentDto>> Handle(GetDepartmentsQuery query, CancellationToken ct)
        => await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                ParentDeptId = d.ParentDepartmentId,
                ParentDeptName = d.ParentDepartment != null
                    ? d.ParentDepartment.Name
                    : null
            })
            .ToListAsync(ct);
}
