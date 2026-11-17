using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Departments.Queries.GetDepartment;

public record GetDepartmentsQuery : IRequest<IReadOnlyCollection<DepartmentDto>>;

public class GetDepartmentsHandler(PayClaimDbContext db, IMemoryCache cache)
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyCollection<DepartmentDto>>
{
    public const string CacheKey = "master:departments";

    public async Task<IReadOnlyCollection<DepartmentDto>> Handle(GetDepartmentsQuery query, CancellationToken ct)
        => (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await db.Departments
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
        }))!;
}
