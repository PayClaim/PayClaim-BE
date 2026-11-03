
using Microsoft.EntityFrameworkCore;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(PayClaimDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsByNumberAsync(string number, Guid? excludeEmployeeId = null, CancellationToken ct = default)
    {
        return await _context.Employees.AnyAsync(
            e => e.Number == number 
            && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value),ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeEmployeeId = null, CancellationToken ct = default)
    {
        return await _context.Employees.AnyAsync(
            e => e.Email == email
                && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value), ct);
    }
}
