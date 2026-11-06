using PayClaim.Enums;

namespace PayClaim.Entities;

public class TripApproval
{
    public int Id { get; set; }

    public Guid TripProposalId { get; set; }

    public Guid ApproverId { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public TripApprovalDecision Decision { get; set; }

    public string? Notes { get; set; }

    public byte ApprovalLevel { get; set; }

    public DateTime DecidedAt { get; set; }

    public TripProposal TripProposal { get; set; } = null!;

    public Employee Approver { get; set; } = null!;
}