
using PayClaim.Data.Repositories.Interfaces;
using PayClaim.Entities;

namespace PayClaim.Data.UnitOfWork;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IRepository<Department> Departments { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
