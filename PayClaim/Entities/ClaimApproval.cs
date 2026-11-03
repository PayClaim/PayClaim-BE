using PayClaim.Enums;

namespace PayClaim.Entities;

public class ClaimApproval
{
    public int Id { get; set; }

    public Guid ClaimId { get; set; }

    public Guid ApproverId { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public ApprovalDecision Decision { get; set; }

    public string? Notes { get; set; }

    public byte ApprovalLevel { get; set; }

    public DateTime DecidedAt { get; set; }

    public Claim Claim { get; set; } = null!;

    public Employee Approver { get; set; } = null!;
}