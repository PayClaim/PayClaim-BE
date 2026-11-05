using Microsoft.AspNetCore.Http;

namespace PayClaim.DTOs.TripProposal;

public class CompleteTripProposalDto
{
    public IFormFile? ReportFile { get; set; }
    public string? ReportNotes { get; set; }
}
