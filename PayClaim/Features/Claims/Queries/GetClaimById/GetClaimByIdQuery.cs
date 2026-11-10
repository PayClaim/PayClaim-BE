using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.Claim;

namespace PayClaim.Features.Claims.Queries.GetClaimById;

public record GetClaimByIdQuery(
    Guid Id,
    Guid? RequesterEmployeeId = null,
    IReadOnlyCollection<string>? RequesterRoles = null) : IRequest<ClaimDetailDto>;

public class GetClaimByIdHandler(PayClaimDbContext context, ICurrentUser currentUser) : IRequestHandler<GetClaimByIdQuery, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(GetClaimByIdQuery query, CancellationToken ct = default)
    {
        var claimAccess = await context.Claims
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new
            {
                c.EmployeeId,
                ManagerId = c.Employee.ManagerId
            })
            .FirstOrDefaultAsync(ct);

        if (claimAccess == null)
        {
            throw new NotFoundException(
                $"Claim with ID '{query.Id}' was not found.");
        }

        var roles = query.RequesterRoles ?? currentUser.Roles;
        var employeeId = query.RequesterEmployeeId ?? currentUser.EmployeeId;

        var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);

        var isFinance = roles.Contains("Finance", StringComparer.OrdinalIgnoreCase) ||
            roles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);

        var isManager = roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);

        var isOwner = employeeId.HasValue &&
            claimAccess.EmployeeId == employeeId.Value;

        var isManagerOfOwner =
            isManager &&
            employeeId.HasValue &&
            claimAccess.ManagerId == employeeId.Value;

        var hasAccess =
            isAdmin ||
            isFinance ||
            isOwner ||
            isManagerOfOwner;

        if (!hasAccess)
        {
            throw new ForbiddenException(
                "You are not allowed to access this claim.");
        }

        var claim = await context.Claims
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new ClaimDetailDto
            {
                Id = c.Id,
                ClaimType = c.ClaimType,
                Description = c.Description,
                TransactionDate = c.TransactionDate,
                Amount = c.Amount,
                SubmittedAt = c.SubmittedAt,
                Status = c.Status,
                FilePath = c.FilePath,

                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee.FullName,
                EmployeeNumber = c.Employee.Number,
                DepartmentName = c.Employee.Department.Name,

                EmployeeBenefitId = c.EmployeeBenefitId,
                BenefitName = c.EmployeeBenefit != null
                    ? c.EmployeeBenefit.Benefit.Name
                    : null,

                TripProposalId = c.TripProposalId
            })
            .FirstOrDefaultAsync(ct);

        if (claim == null)
        {
            throw new NotFoundException(
                $"Claim with ID '{query.Id}' was not found.");
        }

        var approvalHistory = await context.ClaimApprovals
            .AsNoTracking()
            .Where(ca => ca.ClaimId == query.Id)
            .OrderBy(ca => ca.ApprovalLevel)
            .ThenBy(ca => ca.DecidedAt)
            .Select(ca => new ClaimApprovalHistoryDto
            {
                Id = ca.Id,
                ApproverId = ca.ApproverId,
                ApproverName = ca.ApproverName,
                Decision = ca.Decision,
                Notes = ca.Notes,
                ApprovalLevel = ca.ApprovalLevel,
                DecidedAt = ca.DecidedAt
            })
            .ToListAsync(ct);

        claim.ApprovalHistory = approvalHistory;

        return claim;
    }
}
