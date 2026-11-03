using PayClaim.Enums;

namespace PayClaim.Entities;

public class Claim
{
    public Guid Id { get; set; }

    public ClaimType ClaimType { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateOnly TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public DateTime SubmittedAt { get; set; }

    public ClaimStatus Status { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }

    public Guid? EmployeeBenefitId { get; set; }

    public Guid? TripProposalId { get; set; }

    public Employee Employee { get; set; } = null!;

    public EmployeeBenefit? EmployeeBenefit { get; set; }

    public TripProposal? TripProposal { get; set; }
}