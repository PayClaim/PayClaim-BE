using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripProposalDetailDto
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

    public List<ParticipantItemDto> Participants { get; set; } = new();

    public List<TripApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}
