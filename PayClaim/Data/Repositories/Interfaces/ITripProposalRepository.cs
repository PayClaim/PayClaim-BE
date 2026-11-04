using PayClaim.Entities;

namespace PayClaim.Data.Repositories.Interfaces;

public interface ITripProposalRepository : IRepository<TripProposal>
{
    Task<TripProposal?> GetWithDetailsByIdAsync(Guid id, CancellationToken ct = default);
}
