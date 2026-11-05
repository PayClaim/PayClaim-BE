using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(PayClaimDbContext context) : base(context)
    {

    }

    public async Task<Role?> GetByNameAsync( string name, CancellationToken ct = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name, ct);
    }


}
