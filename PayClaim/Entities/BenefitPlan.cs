
namespace PayClaim.Entities;

public class BenefitPlan
{
    public string DepartmentId { get; set; } = string.Empty;

    public Guid BenefitId { get; set; }

    public string PositionId { get; set; } = string.Empty;

    public decimal LimitAmount { get; set; }


    // Navigation Properties
    public Department Department { get; set; } = null!;

    public Benefit Benefit { get; set; } = null!;

    public Position Position { get; set; } = null!;
}
