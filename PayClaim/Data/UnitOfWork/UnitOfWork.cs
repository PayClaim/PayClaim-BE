
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
    public IRepository<Position> Positions { get; }
    public IRepository<BenefitCategory> BenefitCategories { get; }
    public IRepository<Benefit> Benefits { get; }
    public IRepository<BenefitPlan> BenefitPlans { get; }
    public IRepository<AccountRole> AccountRoles { get; }
    public IRepository<EmployeeBenefit> EmployeeBenefits { get; }

    public UnitOfWork(
        PayClaimDbContext context,
        IEmployeeRepository employeeRepository,
        IAccountRepository accountRepository)
    {
        _context = context;

        Employees = employeeRepository;
        Accounts = accountRepository;

        //ga butuh pengecheckan khusus makanya langsung di sini sidepartment
        Departments = new Repository<Department>(context);
        Positions = new Repository<Position>(context);
        BenefitCategories = new Repository<BenefitCategory>(context);
        Benefits = new Repository<Benefit>(context);
        BenefitPlans = new Repository<BenefitPlan>(context);
        AccountRoles = new Repository<AccountRole>(context);
        EmployeeBenefits = new Repository<EmployeeBenefit>(context);
    }

    public async Task<int> SaveChangesAsync( CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
