using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.EmployeeBenefit;

//TEST
public class AdjustEmployeeBenefitDto
{
    [Required]
    public decimal AdjustmentAmount { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}