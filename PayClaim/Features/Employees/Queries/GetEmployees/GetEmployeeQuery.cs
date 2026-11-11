using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Data;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery(EmployeeFilterQuery Filter) : IRequest<PagedResponse<EmployeeDto>>;

public class GetEmployeesHandler(PayClaimDbContext db) : IRequestHandler<GetEmployeesQuery, PagedResponse<EmployeeDto>>
{
    public async Task<PagedResponse<EmployeeDto>> Handle(GetEmployeesQuery query, CancellationToken ct)
    {
        var filter = query.Filter;
        var dbQuery = db.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.DepartmentId))
            dbQuery = dbQuery.Where(e => e.DepartmentId == filter.DepartmentId);

        if (!string.IsNullOrWhiteSpace(filter.PositionId))
            dbQuery = dbQuery.Where(e => e.PositionId == filter.PositionId);

        if (filter.IsActive.HasValue)
            dbQuery = dbQuery.Where(e => e.IsActive == filter.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            dbQuery = dbQuery.Where(e =>
                e.FullName.Contains(s) ||
                e.Number.Contains(s) ||
                e.Email.Contains(s) ||
                e.Department.Name.Contains(s) ||
                e.Position.Title.Contains(s));
        }

        var totalCount = await dbQuery.CountAsync(ct);

        var orderedQuery = dbQuery.OrderBy(e => e.Number);

        int pageNumber = 1;
        int pageSize = totalCount > 0 ? totalCount : 1;

        IQueryable<PayClaim.Entities.Employee> pagedQuery;
        if (filter.PageNumber.HasValue || filter.PageSize.HasValue)
        {
            pageNumber = filter.PageNumber.GetValueOrDefault(1);
            if (pageNumber < 1) pageNumber = 1;

            pageSize = filter.PageSize.GetValueOrDefault(10);
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            pagedQuery = orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
        else
        {
            pagedQuery = orderedQuery;
        }

        var items = await pagedQuery
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

        return new PagedResponse<EmployeeDto>(items, totalCount, pageNumber, pageSize);
    }
}
