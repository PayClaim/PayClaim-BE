using System.ComponentModel.DataAnnotations;
using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripApprovalActionDto
{
    [Required(ErrorMessage = "Decision is required.")]
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Approved;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public Guid? ApproverId { get; set; }

    // Opsional untuk Finance (Level 2) saat menetapkan/menyesuaikan anggaran definitif akomodasi
    public decimal? TotalAmount { get; set; }

    public List<ParticipantItemDto>? Participants { get; set; }
}
