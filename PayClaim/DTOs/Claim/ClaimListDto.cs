using PayClaim.Enums;

namespace PayClaim.DTOs.Claim;

public class ClaimListDto
{
    public Guid Id { get; set; }

    public ClaimType ClaimType { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateOnly TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public DateTime SubmittedAt { get; set; }

    public ClaimStatus Status { get; set; }

    public Guid EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public string? BenefitName { get; set; }

    public string? TripProposalDestination { get; set; }

    public string? TripProposalDescription { get; set; }

    public string? FilePath { get; set; }
}
