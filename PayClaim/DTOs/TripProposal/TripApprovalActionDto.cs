
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

    // Accommodation Budget yang ditentukan oleh Finance.
    public decimal? TotalAmount { get; set; }

    // Digunakan Finance untuk menentukan limit reimbursement setiap peserta.
    public List<ParticipantItemDto>? Participants { get; set; }
}
