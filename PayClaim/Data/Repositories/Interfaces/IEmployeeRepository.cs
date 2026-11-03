using PayClaim.Entities;

namespace PayClaim.Data.Repositories.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    //cek nomor karyawan sm email dah dipake apa blum
    Task<bool> ExistsByNumberAsync(string number, Guid? excludeEmployeeId = null, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeEmployeeId = null, CancellationToken ct = default);
}
