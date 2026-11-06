using System.ComponentModel.DataAnnotations;
using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripApprovalActionDto
{
    [Required(ErrorMessage = "Decision is required.")]
    public TripApprovalDecision Decision { get; set; }
        = TripApprovalDecision.Approved;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public decimal? TotalAmount { get; set; }

    public List<ParticipantItemDto>? Participants { get; set; }
}