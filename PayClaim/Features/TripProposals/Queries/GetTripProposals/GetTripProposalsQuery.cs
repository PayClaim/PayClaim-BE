using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.Common;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Queries.GetTripProposals;

public record GetTripProposalsQuery(
    TripProposalFilterQuery Filter,
    Guid? EmployeeId = null,
    string[]? Roles = null) : IRequest<PagedResponse<TripProposalListDto>>;

public class GetTripProposalsHandler(PayClaimDbContext context, ICurrentUser currentUser) : IRequestHandler<GetTripProposalsQuery, PagedResponse<TripProposalListDto>>
{
    public async Task<PagedResponse<TripProposalListDto>> Handle(
        GetTripProposalsQuery query,
        CancellationToken ct = default)
    {
        var filter = query.Filter;
        var roles = query.Roles ?? currentUser.Roles;
        var employeeId = query.EmployeeId ?? currentUser.EmployeeId;

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

        // 1. Filter berdasarkan hak akses user
        if (!isAdmin)
        {
            if (!employeeId.HasValue ||
                employeeId.Value == Guid.Empty)
            {
                throw new ForbiddenException(
                    "Employee identity is required.");
            }

            var currentEmpId = employeeId.Value;

            dbQuery = dbQuery.Where(tp =>
                // Employee atau participant hanya melihat proposal
                // yang melibatkan dirinya.
                tp.Participants.Any(p =>
                    p.EmployeeId == currentEmpId)

                ||

                // Manager melihat proposal dari direct report (PIC)
                // atau yang pernah diapprove oleh Manager ini
                (
                    isManager &&
                    (
                        tp.Participants.Any(p =>
                            p.IsPic &&
                            p.Employee.ManagerId == currentEmpId)
                        ||
                        context.TripApprovals.Any(ta =>
                            ta.TripProposalId == tp.Id &&
                            ta.ApproverId == currentEmpId)
                    )
                )

                ||

                // Finance dan Finance Manager hanya melihat proposal yang perlu direview
                // atau status setelah direview oleh finance
                (
                    (isFinance || isFinanceManager) &&
                    (
                        tp.Status == TripProposalStatus.UnderReview ||
                        tp.Status == TripProposalStatus.FinanceManagerReview ||
                        tp.Status == TripProposalStatus.Approved ||
                        tp.Status == TripProposalStatus.Completed ||
                        context.TripApprovals.Any(ta =>
                            ta.TripProposalId == tp.Id &&
                            ta.ApprovalLevel >= 2)
                    )
                )
            );
        }

        // 2. Filter pencarian tujuan
        if (!string.IsNullOrWhiteSpace(filter.Destination))
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Destination.Contains(filter.Destination));
        }

        // 3. Filter status
        if (filter.Status.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Status == filter.Status.Value);
        }

        // 4. Filter tanggal mulai
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

        // 5. Filter berdasarkan participant
        if (filter.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(tp =>
                tp.Participants.Any(p =>
                    p.EmployeeId == filter.EmployeeId.Value));
        }

        // 6. Hitung total data sebelum pagination
        var totalCount = await dbQuery.CountAsync(ct);

        // 7. Pagination dan mapping DTO
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
                TotalAmount = tp.TotalAmount +
                    (tp.Participants.Sum(p => (decimal?)p.Amount) ?? 0m),
                SubmittedAt = tp.SubmittedAt,
                Status = tp.Status,

                PicEmployeeId = tp.Participants
                    .Where(p => p.IsPic)
                    .Select(p => (Guid?)p.EmployeeId)
                    .FirstOrDefault(),

                PicName = tp.Participants
                    .Where(p => p.IsPic)
                    .Select(p => p.Employee.FullName)
                    .FirstOrDefault(),

                ParticipantCount = tp.Participants.Count,
                FilePath = tp.FilePath,

                MyParticipantAmount = employeeId.HasValue
                    ? tp.Participants
                        .Where(p => p.EmployeeId == employeeId.Value)
                        .Select(p => (decimal?)p.Amount)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        if (employeeId.HasValue && items.Count > 0)
        {
            var tripIds = items.Select(i => i.Id).ToList();
            var claimedByTrip = await context.Claims
                .Where(c => c.TripProposalId.HasValue &&
                            tripIds.Contains(c.TripProposalId.Value) &&
                            c.EmployeeId == employeeId.Value &&
                            c.Status != ClaimStatus.Rejected)
                .GroupBy(c => c.TripProposalId!.Value)
                .Select(g => new { TripId = g.Key, TotalClaimed = g.Sum(c => c.Amount) })
                .ToDictionaryAsync(x => x.TripId, x => x.TotalClaimed, ct);

            foreach (var item in items)
            {
                if (item.MyParticipantAmount.HasValue)
                {
                    var claimed = claimedByTrip.TryGetValue(item.Id, out var sum) ? sum : 0m;
                    item.MyRemainingBudget = Math.Max(0m, item.MyParticipantAmount.Value - claimed);
                }
            }
        }

        return new PagedResponse<TripProposalListDto>(
            items,
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }
}
