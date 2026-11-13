using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.Claim;
using PayClaim.DTOs.Common;
using PayClaim.Enums;

namespace PayClaim.Features.Claims.Queries.GetClaims;

public record GetClaimsQuery(
    ClaimFilterQuery Filter,
    Guid? RequesterEmployeeId = null,
    IReadOnlyCollection<string>? RequesterRoles = null,
    bool IsMyClaims = false) : IRequest<PagedResponse<ClaimListDto>>;

public class GetClaimsHandler(PayClaimDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetClaimsQuery, PagedResponse<ClaimListDto>>
{
    public async Task<PagedResponse<ClaimListDto>> Handle(
        GetClaimsQuery query,
        CancellationToken ct = default)
    {
        var filter = query.Filter;
        var roles = query.RequesterRoles ?? currentUser.Roles;
        var employeeId = query.RequesterEmployeeId ?? currentUser.EmployeeId;

        var dbQuery = context.Claims
            .AsNoTracking()
            .AsQueryable();

        bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        bool isManager = roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);
        bool isFinance = roles.Contains("Finance", StringComparer.OrdinalIgnoreCase)
                      || roles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);

        if (query.IsMyClaims)
        {
            if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
                throw new ForbiddenException("Employee identity is required to access your claims.");

            dbQuery = dbQuery.Where(c => c.EmployeeId == employeeId.Value);
        }
        else if (!isAdmin)
        {
            if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
                throw new ForbiddenException("Employee identity is required.");

            if (!isManager && !isFinance)
                throw new ForbiddenException("You are not allowed to access all claims.");

            var requesterEmployeeId = employeeId.Value;

            HashSet<Guid> approvedByRequesterClaimIds = [];
            HashSet<Guid> financeVisibleClaimIds = [];

            if (isManager)
            {
                approvedByRequesterClaimIds = await context.ClaimApprovals
                    .AsNoTracking()
                    .Where(ca => ca.ApproverId == requesterEmployeeId)
                    .Select(ca => ca.ClaimId)
                    .ToHashSetAsync(ct);
            }

            if (isFinance)
            {
                financeVisibleClaimIds = await context.ClaimApprovals
                    .AsNoTracking()
                    .Where(ca => ca.ApprovalLevel >= 2)
                    .Select(ca => ca.ClaimId)
                    .ToHashSetAsync(ct);
            }

            dbQuery = dbQuery.Where(c =>
                (
                    isManager &&
                    (
                        c.Employee.ManagerId == requesterEmployeeId ||
                        approvedByRequesterClaimIds.Contains(c.Id)
                    )
                )
                ||
                (
                    isFinance &&
                    (
                        c.Status == ClaimStatus.UnderReview ||
                        c.Status == ClaimStatus.Approved ||
                        c.Status == ClaimStatus.Paid ||
                        financeVisibleClaimIds.Contains(c.Id)
                    )
                )
            );
        }

        // Filter berdasarkan ClaimType
        if (filter.ClaimType.HasValue)
            dbQuery = dbQuery.Where(c => c.ClaimType == filter.ClaimType.Value);

        // Filter berdasarkan status
        if (filter.Status.HasValue)
            dbQuery = dbQuery.Where(c => c.Status == filter.Status.Value);

        // Filter berdasarkan EmployeeId
        if (filter.EmployeeId.HasValue)
            dbQuery = dbQuery.Where(c => c.EmployeeId == filter.EmployeeId.Value);

        // Filter tanggal transaksi
        if (filter.StartDate.HasValue)
            dbQuery = dbQuery.Where(c => c.TransactionDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            dbQuery = dbQuery.Where(c => c.TransactionDate <= filter.EndDate.Value);

        var orderedQuery = dbQuery.OrderByDescending(c => c.SubmittedAt);

        var totalCount = await orderedQuery.CountAsync(ct);

        var items = await orderedQuery
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new ClaimListDto
            {
                Id = c.Id,
                ClaimType = c.ClaimType,
                Description = c.Description,
                TransactionDate = c.TransactionDate,
                Amount = c.Amount,
                SubmittedAt = c.SubmittedAt,
                Status = c.Status,
                EmployeeId = c.EmployeeId,
                EmployeeName = c.Employee.FullName,
                BenefitName = c.EmployeeBenefit != null
                    ? c.EmployeeBenefit.Benefit.Name
                    : null,
                TripProposalDestination = c.TripProposal != null
                    ? c.TripProposal.Destination
                    : null,
                TripProposalDescription = c.TripProposal != null
                    ? c.TripProposal.Description
                    : null,
                FilePath = c.FilePath
            })
            .ToListAsync(ct);

        return new PagedResponse<ClaimListDto>(
            items,
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }
}
