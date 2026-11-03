using PayClaim.DTOs.Common;
using PayClaim.Enums;

namespace PayClaim.DTOs.TripProposal;

public class TripProposalFilterQuery : PaginationQuery
{
    public TripProposalStatus? Status { get; set; }

    public DateOnly? StartDateFrom { get; set; }

    public DateOnly? StartDateTo { get; set; }
}
