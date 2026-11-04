using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Auth;

namespace PayClaim.Features.Auth.Queries.GetMe;

public class GetMeQuery
{
    public Guid AccountId { get; set; }

    public GetMeQuery(Guid accountId)
    {
        AccountId = accountId;
    }
}

public class GetMeHandler
{
    private readonly PayClaimDbContext _context;

    public GetMeHandler(PayClaimDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileResponseDto> Handle(GetMeQuery query, CancellationToken ct = default)
    {
        var userProfile = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == query.AccountId && a.IsActive)
            .Select(a => new UserProfileResponseDto
            {
                AccountId = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeNumber = a.Employee.Number,
                Username = a.Username,
                FullName = a.Employee.FullName,
                Email = a.Employee.Email,
                DepartmentId = a.Employee.DepartmentId,
                DepartmentName = a.Employee.Department.Name,
                PositionId = a.Employee.PositionId,
                PositionTitle = a.Employee.Position.Title,
                ManagerId = a.Employee.ManagerId,
                ManagerName = a.Employee.Manager != null ? a.Employee.Manager.FullName : null,
                Roles = a.AccountRoles.Select(ar => ar.Role.Name).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (userProfile == null)
        {
            throw new NotFoundException("User profile not found or account is inactive.");
        }

        return userProfile;
    }
}
