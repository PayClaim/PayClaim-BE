using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.Repositories;

public class TripProposalRepository : Repository<TripProposal>, ITripProposalRepository
{
    public TripProposalRepository(PayClaimDbContext context) : base(context)
    {
    }

    public async Task<TripProposal?> GetWithDetailsByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(tp => tp.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);
    }
}
