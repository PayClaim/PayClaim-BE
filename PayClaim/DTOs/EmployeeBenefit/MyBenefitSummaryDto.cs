using PayClaim.Enums;

namespace PayClaim.DTOs.EmployeeBenefit;

public class MyBenefitSummaryDto
{
    public Guid EmployeeBenefitId { get; set; }

    public Guid BenefitId { get; set; }

    public string BenefitName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public BenefitPeriodType PeriodType { get; set; }

    public decimal Balance { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }
    //coba
    public string EmployeeName { get; set; } = string.Empty;
}