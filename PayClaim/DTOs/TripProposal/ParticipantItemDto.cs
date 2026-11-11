using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.TripProposal;

public class ParticipantItemDto
{
    [Required(ErrorMessage = "EmployeeId is required.")]
    public Guid EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public string? EmployeeNumber { get; set; }

    public bool IsPic { get; set; }

    // Limit reimbursement untuk satu peserta selama seluruh perjalanan.
    // Saat Create, nilainya 0 karena akan ditentukan oleh Finance.
    [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative.")]
    public decimal Amount { get; set; } = 0;
}