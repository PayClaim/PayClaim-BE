using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripProposalDetailDto
{
    public Guid Id { get; set; }

    public string Destination { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    // Biaya akomodasi yang tersimpan di kolom TotalAmount Entity.
    public decimal AccommodationAmount { get; set; }

    // Total biaya akomodasi + limit seluruh peserta.
    public decimal TotalAmount { get; set; }

    public decimal FinanceLimit { get; set; }

    public DateTime SubmittedAt { get; set; }

    public string Description { get; set; } = string.Empty;

    public TripProposalStatus Status { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public List<ParticipantItemDto> Participants { get; set; } = new();

    public List<TripApprovalHistoryDto> ApprovalHistory { get; set; } = new();

    public decimal? MyParticipantAmount { get; set; }

    public decimal? MyClaimedAmount { get; set; }

    public decimal? MyRemainingBudget { get; set; }
}