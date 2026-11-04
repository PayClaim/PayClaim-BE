
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.UnitOfWork;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IRepository<Department> Departments { get; }
    IRepository<Position> Positions { get; }
    IRepository<BenefitCategory> BenefitCategories { get; }
    IRepository<Benefit> Benefits { get; }
    IRepository<BenefitPlan> BenefitPlans { get; }
    IRepository<Account> Accounts { get; }
    IRepository<AccountRole> AccountRoles { get; }
    IRepository<EmployeeBenefit> EmployeeBenefits { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
