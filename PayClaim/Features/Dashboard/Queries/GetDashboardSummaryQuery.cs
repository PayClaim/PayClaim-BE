using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.DTOs.Dashboard;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryHandler(
    PayClaimDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private static readonly CultureInfo IdCulture = CultureInfo.GetCultureInfo("id-ID");

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery query,
        CancellationToken ct = default)
    {
        var roles = currentUser.Roles;
        var employeeId = currentUser.EmployeeId;

        bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        bool isFinanceManager = roles.Contains("Finance Manager", StringComparer.OrdinalIgnoreCase);
        bool isFinance = roles.Contains("Finance", StringComparer.OrdinalIgnoreCase) && !isFinanceManager;
        bool isManager = (roles.Contains("Manager", StringComparer.OrdinalIgnoreCase) ||
                          roles.Contains("Project Manager", StringComparer.OrdinalIgnoreCase)) && !isFinanceManager;

        bool isApprover = isAdmin || isManager || isFinance || isFinanceManager;

        if (!isAdmin && (!employeeId.HasValue || employeeId.Value == Guid.Empty))
        {
            throw new ForbiddenException("Employee identity is required.");
        }

        var currentEmpId = employeeId ?? Guid.Empty;
        var jakartaNow = DateTime.UtcNow.AddHours(7);
        var todayStart = jakartaNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var todayDateOnly = DateOnly.FromDateTime(todayStart);

        var result = new DashboardSummaryDto();

        // 1. Base Queries (visibility-scoped)
        var myClaimsQuery = currentEmpId != Guid.Empty
            ? context.Claims.AsNoTracking().Where(c => c.EmployeeId == currentEmpId)
            : context.Claims.AsNoTracking().Where(_ => false);

        IQueryable<Claim> visibleClaimsQuery = isApprover
            ? BuildVisibleClaimsQuery(isAdmin, isManager, isFinance || isFinanceManager, currentEmpId)
            : myClaimsQuery;

        var myProposalsQuery = currentEmpId != Guid.Empty
            ? context.TripProposals.AsNoTracking()
                .Where(tp => tp.Participants.Any(p => p.IsPic && p.EmployeeId == currentEmpId))
            : context.TripProposals.AsNoTracking().Where(_ => false);

        var visibleProposalsQuery = BuildVisibleProposalsQuery(
            isAdmin, isManager, isFinance, isFinanceManager, currentEmpId);

        // 2. Today Summary Counts (SQL COUNT)
        if (!isAdmin)
        {
            result.TodaySummary.MyAttentionClaimsCount = await myClaimsQuery
                .CountAsync(c => c.Status == ClaimStatus.NeedsRevision || c.Status == ClaimStatus.Rejected, ct);

            result.TodaySummary.MyAttentionProposalsCount = await myProposalsQuery
                .CountAsync(tp => tp.Status == TripProposalStatus.NeedsRevision || tp.Status == TripProposalStatus.Rejected, ct);

            result.TodaySummary.MyUpdatedClaimsCount = await myClaimsQuery
                .CountAsync(c => context.ClaimApprovals.Any(ca =>
                    ca.ClaimId == c.Id &&
                    ca.DecidedAt >= todayStart &&
                    ca.DecidedAt < todayEnd), ct);

            result.TodaySummary.MyUpdatedProposalsCount = await myProposalsQuery
                .CountAsync(tp => context.TripApprovals.Any(ta =>
                    ta.TripProposalId == tp.Id &&
                    ta.DecidedAt >= todayStart &&
                    ta.DecidedAt < todayEnd), ct);
        }

        if (isApprover)
        {
            var approvableClaimsQuery = visibleClaimsQuery.Where(c => c.EmployeeId != currentEmpId);
            result.TodaySummary.PendingClaimApprovalsCount = isAdmin
                ? await approvableClaimsQuery.CountAsync(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview, ct)
                : isManager
                    ? await approvableClaimsQuery.CountAsync(c => c.Status == ClaimStatus.Submitted, ct)
                    : isFinance
                        ? await approvableClaimsQuery.CountAsync(c => c.Status == ClaimStatus.UnderReview, ct)
                        : 0;

            var approvableProposalsQuery = visibleProposalsQuery
                .Where(tp => !tp.Participants.Any(p => p.IsPic && p.EmployeeId == currentEmpId));
            result.TodaySummary.PendingTripApprovalsCount = isAdmin
                ? await approvableProposalsQuery.CountAsync(tp =>
                    tp.Status == TripProposalStatus.Submitted ||
                    tp.Status == TripProposalStatus.UnderReview ||
                    tp.Status == TripProposalStatus.FinanceManagerReview, ct)
                : isManager
                    ? await approvableProposalsQuery.CountAsync(tp => tp.Status == TripProposalStatus.Submitted, ct)
                    : isFinance
                        ? await approvableProposalsQuery.CountAsync(tp => tp.Status == TripProposalStatus.UnderReview, ct)
                        : isFinanceManager
                            ? await approvableProposalsQuery.CountAsync(tp => tp.Status == TripProposalStatus.FinanceManagerReview, ct)
                            : 0;
        }

        // 3. Benefits (for non-Admin)
        if (!isAdmin && currentEmpId != Guid.Empty)
        {
            result.Benefits = await context.EmployeeBenefits
                .AsNoTracking()
                .Where(eb => eb.EmployeeId == currentEmpId)
                .OrderBy(eb => eb.Benefit.Name)
                .Select(eb => new DashboardBenefitDto
                {
                    EmployeeBenefitId = eb.Id,
                    BenefitName = eb.Benefit.Name,
                    Balance = eb.Balance,
                    PeriodType = eb.Benefit.PeriodType,
                    StartDate = eb.StartDate,
                    EndDate = eb.EndDate
                })
                .ToListAsync(ct);
        }

        // 4. Overview Status Distribution (for Approver, 1 SQL GROUP BY each)
        if (isApprover)
        {
            var claimStatusGroups = await visibleClaimsQuery
                .GroupBy(c => new { c.Status, c.SubmittedAt.Year, c.SubmittedAt.Month })
                .Select(g => new StatusMonthGroup<ClaimStatus>(g.Key.Status, g.Key.Year, g.Key.Month, g.Count()))
                .ToListAsync(ct);

            result.ClaimStatusOverview = BuildStatusOverview(claimStatusGroups, todayDateOnly);

            var tripStatusGroups = await visibleProposalsQuery
                .GroupBy(tp => new { tp.Status, tp.SubmittedAt.Year, tp.SubmittedAt.Month })
                .Select(g => new StatusMonthGroup<TripProposalStatus>(g.Key.Status, g.Key.Year, g.Key.Month, g.Count()))
                .ToListAsync(ct);

            result.TripStatusOverview = BuildStatusOverview(tripStatusGroups, todayDateOnly);
        }

        // 5. Expense Trends (1 SQL GROUP BY each)
        if (!isAdmin)
        {
            var myPaidGroups = await GetPaidClaimGroupsAsync(myClaimsQuery, ct);
            result.MyExpenseTrend = BuildExpenseTrendByPeriod(myPaidGroups, todayDateOnly);
        }

        if (isApprover)
        {
            var companyPaidGroups = await GetPaidClaimGroupsAsync(visibleClaimsQuery, ct);
            result.CompanyExpenseTrend = BuildExpenseTrendByPeriod(companyPaidGroups, todayDateOnly);
        }

        // 6. Recent Actions (Top 5 combined from top 5 claims + top 5 proposals)
        var topClaimsQuery = (isApprover
                ? context.Claims.AsNoTracking().Where(c =>
                    c.EmployeeId == currentEmpId ||
                    visibleClaimsQuery.Any(vc => vc.Id == c.Id))
                : myClaimsQuery)
            .OrderByDescending(c => c.SubmittedAt)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                c.ClaimType,
                c.Description,
                c.TransactionDate,
                c.Amount,
                c.SubmittedAt,
                c.Status,
                c.EmployeeId,
                EmployeeName = c.Employee.FullName,
                BenefitName = c.EmployeeBenefit != null ? c.EmployeeBenefit.Benefit.Name : null,
                TripProposalDescription = c.TripProposal != null ? c.TripProposal.Description : null
            });

        var topClaims = await topClaimsQuery.ToListAsync(ct);

        var topProposals = await visibleProposalsQuery
            .OrderByDescending(tp => tp.SubmittedAt)
            .Take(5)
            .Select(tp => new
            {
                tp.Id,
                tp.Destination,
                tp.Status,
                TotalAmount = tp.TotalAmount + (tp.Participants.Sum(p => (decimal?)p.Amount) ?? 0m),
                tp.SubmittedAt,
                IsOwn = currentEmpId != Guid.Empty && tp.Participants.Any(p => p.IsPic && p.EmployeeId == currentEmpId)
            })
            .ToListAsync(ct);

        var recentClaims = topClaims.Select(c =>
        {
            var isOwn = c.EmployeeId == currentEmpId;
            return new DashboardRecentActionDto
            {
                Id = c.Id,
                Type = "Claim",
                Title = !string.IsNullOrWhiteSpace(c.BenefitName)
                    ? c.BenefitName
                    : !string.IsNullOrWhiteSpace(c.TripProposalDescription)
                        ? c.TripProposalDescription
                        : !string.IsNullOrWhiteSpace(c.Description)
                            ? c.Description
                            : "Reimbursement Claim",
                Description = isOwn
                    ? $"{c.ClaimType} · {c.TransactionDate.ToString("dd MMM yyyy", IdCulture)}"
                    : $"{c.EmployeeName} · {c.ClaimType}",
                Status = c.Status.ToString(),
                Amount = c.Amount,
                Date = c.SubmittedAt,
                Href = isApprover && !isOwn ? $"/approvals/claims/{c.Id}" : $"/claims/{c.Id}"
            };
        });

        var recentProposals = topProposals.Select(tp => new DashboardRecentActionDto
        {
            Id = tp.Id,
            Type = "Trip",
            Title = !string.IsNullOrWhiteSpace(tp.Destination) ? tp.Destination : "Business Trip",
            Description = $"Business Trip · {tp.SubmittedAt.ToString("dd MMM yyyy", IdCulture)}",
            Status = tp.Status.ToString(),
            Amount = tp.TotalAmount,
            Date = tp.SubmittedAt,
            Href = isApprover && !tp.IsOwn ? $"/approvals/trip-proposals/{tp.Id}" : $"/trip-proposals/{tp.Id}"
        });

        result.RecentActions = recentClaims
            .Concat(recentProposals)
            .OrderByDescending(a => a.Date)
            .Take(5)
            .ToList();

        return result;
    }

    private IQueryable<Claim> BuildVisibleClaimsQuery(
        bool isAdmin,
        bool isManager,
        bool isFinance,
        Guid currentEmpId)
    {
        var dbQuery = context.Claims.AsNoTracking();
        if (isAdmin) return dbQuery;

        return dbQuery.Where(c =>
            (
                isManager &&
                (
                    c.Employee.ManagerId == currentEmpId ||
                    context.ClaimApprovals.Any(ca =>
                        ca.ClaimId == c.Id &&
                        ca.ApproverId == currentEmpId)
                )
            )
            ||
            (
                isFinance &&
                (
                    c.Status == ClaimStatus.UnderReview ||
                    c.Status == ClaimStatus.Approved ||
                    c.Status == ClaimStatus.Paid ||
                    context.ClaimApprovals.Any(ca =>
                        ca.ClaimId == c.Id &&
                        ca.ApprovalLevel >= 2)
                )
            ));
    }

    private IQueryable<TripProposal> BuildVisibleProposalsQuery(
        bool isAdmin,
        bool isManager,
        bool isFinance,
        bool isFinanceManager,
        Guid currentEmpId)
    {
        var dbQuery = context.TripProposals.AsNoTracking();
        if (isAdmin) return dbQuery;

        return dbQuery.Where(tp =>
            tp.Participants.Any(p => p.EmployeeId == currentEmpId)
            ||
            (
                isManager &&
                (
                    tp.Participants.Any(p => p.IsPic && p.Employee.ManagerId == currentEmpId) ||
                    context.TripApprovals.Any(ta =>
                        ta.TripProposalId == tp.Id &&
                        ta.ApproverId == currentEmpId)
                )
            )
            ||
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
            ));
    }

    private sealed record StatusMonthGroup<TStatus>(TStatus Status, int Year, int Month, int Count);

    private static Dictionary<string, Dictionary<string, int>> BuildStatusOverview<TStatus>(
        List<StatusMonthGroup<TStatus>> groups,
        DateOnly today) where TStatus : struct, Enum
    {
        var sixMonthsStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);

        bool Matches(int year, int month, string period) => period switch
        {
            "month" => year == today.Year && month == today.Month,
            "sixMonths" => new DateOnly(year, month, 1) >= sixMonthsStart,
            "year" => year == today.Year,
            _ => true
        };

        var result = new Dictionary<string, Dictionary<string, int>>();
        foreach (var period in new[] { "month", "sixMonths", "year", "all" })
        {
            result[period] = groups
                .Where(g => Matches(g.Year, g.Month, period))
                .GroupBy(g => g.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        }

        return result;
    }

    private sealed record PaidClaimGroup(
        int Year,
        int Month,
        int Day,
        decimal TotalAmount,
        int Count,
        decimal MaxAmount,
        decimal MinAmount);

    private static async Task<List<PaidClaimGroup>> GetPaidClaimGroupsAsync(
        IQueryable<Claim> claimsQuery,
        CancellationToken ct)
    {
        return await claimsQuery
            .Where(c => c.Status == ClaimStatus.Paid)
            .GroupBy(c => new
            {
                c.TransactionDate.Year,
                c.TransactionDate.Month,
                c.TransactionDate.Day
            })
            .Select(g => new PaidClaimGroup(
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Sum(c => c.Amount),
                g.Count(),
                g.Max(c => c.Amount),
                g.Min(c => c.Amount)))
            .ToListAsync(ct);
    }

    private static Dictionary<string, ExpenseTrendPeriodDto> BuildExpenseTrendByPeriod(
        List<PaidClaimGroup> groups,
        DateOnly today)
    {
        var sixMonthsStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);

        return new Dictionary<string, ExpenseTrendPeriodDto>
        {
            ["month"] = BuildPeriodTrend(
                groups.Where(g => g.Year == today.Year && g.Month == today.Month).ToList(),
                filtered => Enumerable.Range(0, 5).Select(weekIdx => new ExpenseBucketDto
                {
                    Key = $"week-{weekIdx + 1}",
                    Label = $"Week {weekIdx + 1}",
                    Amount = filtered
                        .Where(g => Math.Min(4, (g.Day - 1) / 7) == weekIdx)
                        .Sum(g => g.TotalAmount)
                }).ToList()),

            ["sixMonths"] = BuildPeriodTrend(
                groups.Where(g => new DateOnly(g.Year, g.Month, 1) >= sixMonthsStart).ToList(),
                filtered => Enumerable.Range(0, 6).Select(idx =>
                {
                    var d = sixMonthsStart.AddMonths(idx);
                    var key = $"{d.Year}-{d.Month:D2}";
                    return new ExpenseBucketDto
                    {
                        Key = key,
                        Label = d.ToString("MMM", IdCulture),
                        Amount = filtered
                            .Where(g => g.Year == d.Year && g.Month == d.Month)
                            .Sum(g => g.TotalAmount)
                    };
                }).ToList()),

            ["year"] = BuildPeriodTrend(
                groups.Where(g => g.Year == today.Year).ToList(),
                filtered => Enumerable.Range(1, today.Month).Select(m =>
                {
                    var d = new DateOnly(today.Year, m, 1);
                    return new ExpenseBucketDto
                    {
                        Key = $"{today.Year}-{m:D2}",
                        Label = d.ToString("MMM", IdCulture),
                        Amount = filtered
                            .Where(g => g.Month == m)
                            .Sum(g => g.TotalAmount)
                    };
                }).ToList()),

            ["all"] = BuildPeriodTrend(
                groups,
                filtered => filtered
                    .GroupBy(g => g.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new ExpenseBucketDto
                    {
                        Key = g.Key.ToString(),
                        Label = g.Key.ToString(),
                        Amount = g.Sum(x => x.TotalAmount)
                    })
                    .ToList())
        };
    }

    private static ExpenseTrendPeriodDto BuildPeriodTrend(
        List<PaidClaimGroup> filtered,
        Func<List<PaidClaimGroup>, List<ExpenseBucketDto>> buildBuckets)
    {
        if (filtered.Count == 0)
        {
            return new ExpenseTrendPeriodDto
            {
                TotalPaid = 0,
                PaidCount = 0,
                HighestAmount = null,
                LowestAmount = null,
                Buckets = buildBuckets(filtered)
            };
        }

        return new ExpenseTrendPeriodDto
        {
            TotalPaid = filtered.Sum(g => g.TotalAmount),
            PaidCount = filtered.Sum(g => g.Count),
            HighestAmount = filtered.Max(g => g.MaxAmount),
            LowestAmount = filtered.Min(g => g.MinAmount),
            Buckets = buildBuckets(filtered)
        };
    }
}
