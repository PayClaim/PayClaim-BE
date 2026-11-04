using PayClaim.Enums;

namespace PayClaim.Entities;

public class TripProposal
{
    public Guid Id { get; set; }

    public string Destination { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string Description { get; set; } = string.Empty;

    public TripProposalStatus Status { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public ICollection<ProposalParticipant> Participants { get; set; }
        = new List<ProposalParticipant>();
}
