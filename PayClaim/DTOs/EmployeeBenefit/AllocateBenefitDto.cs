using System.ComponentModel.DataAnnotations;
using PayClaim.DTOs.Common;

namespace PayClaim.DTOs.EmployeeBenefit;

public class AllocateBenefitDto
{
    [Required(ErrorMessage = "Employee ID is required.")]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Benefit ID is required.")]
    public Guid BenefitId { get; set; }

    [Required(ErrorMessage = "Initial balance is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Initial balance must be greater than 0.")]
    public decimal InitialBalance { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly EndDate { get; set; }
}

public class EmployeeBenefitFilterQuery : PaginationQuery
{
    public Guid? EmployeeId { get; set; }

    public Guid? BenefitId { get; set; }
}
