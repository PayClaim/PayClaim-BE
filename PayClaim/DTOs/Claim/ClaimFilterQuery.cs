using PayClaim.DTOs.Common;
using PayClaim.Enums;

namespace PayClaim.DTOs.Claim;

public class ClaimFilterQuery : PaginationQuery
{
    public ClaimType? ClaimType { get; set; }

    public ClaimStatus? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public Guid? EmployeeId { get; set; }
}
