using System.ComponentModel.DataAnnotations;
using PayClaim.DTOs.Common;

namespace PayClaim.DTOs.Master;

public class BenefitPlanDto
{
    public string DepartmentId { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public string PositionId { get; set; } = string.Empty;

    public string? PositionTitle { get; set; }

    public Guid BenefitId { get; set; }

    public string? BenefitName { get; set; }

    public decimal LimitAmount { get; set; }
}

public class UpsertBenefitPlanDto
{
    [Required(ErrorMessage = "Department ID is required.")]
    public string DepartmentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position ID is required.")]
    public string PositionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Benefit ID is required.")]
    public Guid BenefitId { get; set; }

    [Required(ErrorMessage = "Limit amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Limit amount must be greater than 0.")]
    public decimal LimitAmount { get; set; }
}

public class BenefitPlanFilterQuery
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Search { get; set; }

    public string? DepartmentId { get; set; }

    public string? PositionId { get; set; }

    public Guid? BenefitId { get; set; }
}
