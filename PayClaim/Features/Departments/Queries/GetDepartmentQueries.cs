
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Master.Departments.Queries.GetDepartments;

public class GetDepartmentsQuery
{
}

public class GetDepartmentsHandler
{
    private readonly PayClaimDbContext _context;

    public GetDepartmentsHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery query,CancellationToken ct = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
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

        return departments;
    }
}
