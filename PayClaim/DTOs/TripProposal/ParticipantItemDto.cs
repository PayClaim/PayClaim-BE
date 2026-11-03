using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.TripProposal;

public class ParticipantItemDto
{
    [Required(ErrorMessage = "EmployeeId is required.")]
    public Guid EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public string? EmployeeNumber { get; set; }

    public bool IsPic { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }
}
