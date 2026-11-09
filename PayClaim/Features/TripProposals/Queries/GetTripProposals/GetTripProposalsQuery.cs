using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposals;

public record GetTripProposalsQuery(
    TripProposalFilterQuery Filter,
    Guid? EmployeeId,
    string[] Roles);

public class GetTripProposalsHandler(PayClaimDbContext context)
{
    public async Task<PagedResponse<TripProposalListDto>> Handle(
        GetTripProposalsQuery query,
        CancellationToken ct = default)
    {
        var filter = query.Filter;
        var roles = query.Roles ?? Array.Empty<string>();

        bool isAdmin = roles.Contains(
            "Admin", StringComparer.OrdinalIgnoreCase);

        bool isManager = roles.Contains(
            "Manager", StringComparer.OrdinalIgnoreCase);

        bool isFinance = roles.Contains(
            "Finance", StringComparer.OrdinalIgnoreCase);

        bool isFinanceManager = roles.Contains(
            "Finance Manager", StringComparer.OrdinalIgnoreCase);

        var dbQuery = context.TripProposals
            .AsNoTracking()
            .AsQueryable();

        if (!isAdmin)
        {
            if (!query.EmployeeId.HasValue ||
                query.EmployeeId.Value == Guid.Empty)
            {
                throw new ForbiddenException(
                    "Employee identity is required.");
            }

            var employeeId = query.EmployeeId.Value;

            dbQuery = dbQuery.Where(tp =>
                tp.Participants.Any(p =>
                    p.EmployeeId == employeeId)
                ||
                (
                    isManager &&
                    tp.Status == TripProposalStatus.Submitted &&
                    tp.Participants.Any(p =>
                        p.IsPic &&
                        p.Employee.ManagerId == employeeId)
                )
                ||
                (
                    isFinance &&
                    tp.Status == TripProposalStatus.UnderReview
                )
                ||
                (
                    isFinanceManager &&
                    tp.Status == TripProposalStatus.FinanceManagerReview
                )
            );
        }

            dbQuery = dbQuery.Where(tp =>
                tp.Participants.Any(p =>
                    p.EmployeeId == employeeId)
                ||
                (
                    isManager &&
                    tp.Status == TripProposalStatus.Submitted &&
                    tp.Participants.Any(p =>
                        p.IsPic &&
                        p.Employee.ManagerId == employeeId)
                )
                ||
                (
                    isFinance &&
                    tp.Status == TripProposalStatus.UnderReview
                )
                ||
                (
                    isFinanceManager &&
                    tp.Status == TripProposalStatus.FinanceManagerReview
                )
            );
        }

        // 1. Filter Pencarian Tujuan
        if (!string.IsNullOrWhiteSpace(filter.Destination))
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Destination.Contains(filter.Destination));
        }

        if (filter.Status.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Status == filter.Status.Value);
        }

        if (filter.StartDateFrom.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.StartDate >= filter.StartDateFrom.Value);
        }

        if (filter.StartDateTo.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.StartDate <= filter.StartDateTo.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Participants.Any(p =>
                    p.EmployeeId == filter.EmployeeId.Value));
        }

        var totalCount = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .OrderByDescending(tp => tp.SubmittedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(tp => new TripProposalListDto
            {
                Id = tp.Id,
                Destination = tp.Destination,
                StartDate = tp.StartDate,
                EndDate = tp.EndDate,
                TotalAmount = tp.TotalAmount,
                SubmittedAt = tp.SubmittedAt,
                Status = tp.Status,

                PicName = tp.Participants
                    .Where(p => p.IsPic)
                    .Select(p => p.Employee.FullName)
                    .FirstOrDefault(),

                ParticipantCount = tp.Participants.Count,
                FilePath = tp.FilePath
            })
            .ToListAsync(ct);

        return new PagedResponse<TripProposalListDto>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }
}