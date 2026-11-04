using PayClaim.Data.Repositories;
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly PayClaimDbContext _context;

    public IEmployeeRepository Employees { get; }
    public IAccountRepository Accounts { get; }
    public IRepository<Department> Departments { get; }

    public UnitOfWork(
        PayClaimDbContext context,
        IEmployeeRepository employeeRepository,
        IAccountRepository accountRepository)
    {
        _context = context;
        Employees = employeeRepository;
        Accounts = accountRepository;
        Departments = new Repository<Department>(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
