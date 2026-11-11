using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripProposalListDto
{
    public Guid Id { get; set; }

    public string Destination { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime SubmittedAt { get; set; }

    public TripProposalStatus Status { get; set; }

    public Guid? PicEmployeeId { get; set; }

    public string? PicName { get; set; }

    public int ParticipantCount { get; set; }

    public string? FilePath { get; set; }
}
