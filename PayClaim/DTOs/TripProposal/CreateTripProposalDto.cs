using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.TripProposal;

public class CreateTripProposalDto
{
    [Required(ErrorMessage = "Destination is required.")]
    [StringLength(150, ErrorMessage = "Destination cannot exceed 150 characters.")]
    public string Destination { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateOnly EndDate { get; set; }

    [Required(ErrorMessage = "Total amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0.")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one participant is required.")]
    [MinLength(1, ErrorMessage = "At least one participant is required.")]
    public List<ParticipantItemDto> Participants { get; set; } = new();

    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
}
