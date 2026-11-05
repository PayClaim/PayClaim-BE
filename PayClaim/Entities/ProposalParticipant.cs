namespace PayClaim.Entities;

public class ProposalParticipant
{
    public int Id { get; set; }

    public Guid TripProposalId { get; set; }

    public Guid? EmployeeId { get; set; }

    public bool IsPic { get; set; }

    public decimal Amount { get; set; }

    public TripProposal TripProposal { get; set; } = null!;

    public Employee Employee { get; set; } = null!;
}