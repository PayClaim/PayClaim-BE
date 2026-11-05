using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PayClaim.Enums;

namespace PayClaim.DTOs.Claim;

public class CreateClaimDto
{
    [Required(ErrorMessage = "Claim type is required.")]
    public ClaimType ClaimType { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction date is required.")]
    public DateOnly TransactionDate { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public Guid? EmployeeBenefitId { get; set; }

    public Guid? TripProposalId { get; set; }

    public IFormFile? ReceiptFile { get; set; }

    public Guid? EmployeeId { get; set; }
}
