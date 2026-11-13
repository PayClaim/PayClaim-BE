using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(PayClaimDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByUsernameWithDetailsAsync(string username, CancellationToken ct = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .Include(a => a.Employee)
                .ThenInclude(e => e.Position)
            .Include(a => a.Employee)
                .ThenInclude(e => e.Manager)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Username == username, ct);
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _context.Accounts.AnyAsync(a => a.Username == username, ct);
    }

    public async Task<Account?> GetByEmployeeIdWithRolesAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await _context.Accounts
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId, ct);
    }

    public async Task<Account?> GetByIdentifierAsync(string identifier, CancellationToken ct = default)
    {
        var trimmed = identifier.Trim();
        return await _context.Accounts
            .Include(a => a.Employee)
            .Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
            .FirstOrDefaultAsync(a => a.Username == trimmed || (a.Employee != null && a.Employee.Email == trimmed), ct);
    }
}
