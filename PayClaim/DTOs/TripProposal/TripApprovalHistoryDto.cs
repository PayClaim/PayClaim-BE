using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripApprovalHistoryDto
{
    public int Id { get; set; }

    public Guid ApproverId { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public ApprovalDecision Decision { get; set; }

    public string? Notes { get; set; }

    public byte ApprovalLevel { get; set; }

    public DateTime DecidedAt { get; set; }
}
