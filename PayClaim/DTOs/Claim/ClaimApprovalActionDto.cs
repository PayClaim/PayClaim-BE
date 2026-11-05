using System.ComponentModel.DataAnnotations;
using PayClaim.Enums;

namespace PayClaim.DTOs.Claim;

public class ClaimApprovalActionDto
{
    [Required(ErrorMessage = "Decision is required.")]
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Approved;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public Guid? ApproverId { get; set; }
}
