using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.UnitOfWork;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IAccountRepository Accounts { get; }
    IBenefitPlanRepository BenefitPlans { get; }
    IRoleRepository Roles { get; }

    IRepository<Department> Departments { get; }
    IRepository<Position> Positions { get; }
    IRepository<BenefitCategory> BenefitCategories { get; }
    IRepository<Benefit> Benefits { get; }
    IRepository<AccountRole> AccountRoles { get; }
    IRepository<EmployeeBenefit> EmployeeBenefits { get; }
    //IRepository<Role> Roles { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}