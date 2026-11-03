
using PayClaim.Enums;

namespace PayClaim.Entities;

public class Benefit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BenefitPeriodType PeriodType { get; set; }
    public int CategoryId { get; set; }

    // Navigation Property
    public BenefitCategory Category { get; set; } = null!;
}
