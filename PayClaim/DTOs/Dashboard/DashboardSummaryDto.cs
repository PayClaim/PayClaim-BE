using PayClaim.Enums;

namespace PayClaim.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public DashboardTodaySummaryDto TodaySummary { get; set; } = new();
    public List<DashboardBenefitDto> Benefits { get; set; } = [];
    public Dictionary<string, Dictionary<string, int>> ClaimStatusOverview { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> TripStatusOverview { get; set; } = new();
    public Dictionary<string, ExpenseTrendPeriodDto> MyExpenseTrend { get; set; } = new();
    public Dictionary<string, ExpenseTrendPeriodDto> CompanyExpenseTrend { get; set; } = new();
    public List<DashboardRecentActionDto> RecentActions { get; set; } = [];
}

public class DashboardTodaySummaryDto
{
    public int MyAttentionClaimsCount { get; set; }
    public int MyAttentionProposalsCount { get; set; }
    public int MyUpdatedClaimsCount { get; set; }
    public int MyUpdatedProposalsCount { get; set; }
    public int PendingClaimApprovalsCount { get; set; }
    public int PendingTripApprovalsCount { get; set; }
}

public class DashboardBenefitDto
{
    public Guid EmployeeBenefitId { get; set; }
    public string BenefitName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public BenefitPeriodType PeriodType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ExpenseTrendPeriodDto
{
    public decimal TotalPaid { get; set; }
    public int PaidCount { get; set; }
    public decimal? HighestAmount { get; set; }
    public decimal? LowestAmount { get; set; }
    public List<ExpenseBucketDto> Buckets { get; set; } = [];
}

public class ExpenseBucketDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DashboardRecentActionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Href { get; set; } = string.Empty;
}
