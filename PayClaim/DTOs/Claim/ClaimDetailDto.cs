using PayClaim.Enums;

namespace PayClaim.DTOs.Claim;

public class ClaimDetailDto
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

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;

    public string? ManagerName { get; set; }

    public string? DepartmentName { get; set; }

    public Guid? EmployeeBenefitId { get; set; }

    public string? BenefitName { get; set; }

    public Guid? TripProposalId { get; set; }

    public List<ClaimApprovalHistoryDto> ApprovalHistory { get; set; } = new();
}
