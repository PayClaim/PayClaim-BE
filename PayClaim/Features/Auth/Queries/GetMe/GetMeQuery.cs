using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.Auth;

namespace PayClaim.Features.Auth.Queries.GetMe;

public record GetMeQuery(Guid? AccountId = null) : IRequest<UserProfileResponseDto>;

public class GetMeHandler(PayClaimDbContext context, ICurrentUser currentUser) : IRequestHandler<GetMeQuery, UserProfileResponseDto>
{
    public async Task<UserProfileResponseDto> Handle(GetMeQuery query, CancellationToken ct = default)
    {
        var targetAccountId = query.AccountId ?? currentUser.RequiredAccountId;

        var userProfile = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == targetAccountId && a.IsActive)
            .Select(a => new UserProfileResponseDto
            {
                AccountId = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeNumber = a.Employee != null ? a.Employee.Number : string.Empty,
                Username = a.Username,
                FullName = a.Employee != null ? a.Employee.FullName : string.Empty,
                Email = a.Employee != null ? a.Employee.Email : string.Empty,
                DepartmentId = a.Employee != null ? a.Employee.DepartmentId : string.Empty,
                DepartmentName = a.Employee != null && a.Employee.Department != null ? a.Employee.Department.Name : null,
                PositionId = a.Employee != null ? a.Employee.PositionId : string.Empty,
                PositionTitle = a.Employee != null && a.Employee.Position != null ? a.Employee.Position.Title : null,
                ManagerId = a.Employee != null ? a.Employee.ManagerId : null,
                ManagerName = a.Employee != null && a.Employee.Manager != null ? a.Employee.Manager.FullName : null,
                Roles = a.AccountRoles.Where(ar => ar.Role != null).Select(ar => ar.Role.Name).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (userProfile == null)
        {
            throw new NotFoundException("User profile not found or account is inactive.");
        }

        return userProfile;
    }
}
