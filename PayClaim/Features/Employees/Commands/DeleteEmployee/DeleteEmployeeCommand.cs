using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;

namespace PayClaim.Features.Employees.Commands.DeleteEmployee;

public record DeleteEmployeeCommand(Guid Id) : IRequest;

public class DeleteEmployeeHandler(PayClaimDbContext db) : IRequestHandler<DeleteEmployeeCommand>
{
    public async Task Handle(DeleteEmployeeCommand command, CancellationToken ct)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id, ct);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }

        // 1. Soft Delete: Nonaktifkan status karyawan
        employee.IsActive = false;

        // 2. Putus relasi bawahan jika karyawan ini adalah manager
        var subordinates = await db.Employees
            .Where(e => e.ManagerId == command.Id)
            .ToListAsync(ct);

        foreach (var sub in subordinates)
        {
            sub.ManagerId = null;
        }

        // 3. Nonaktifkan (expire) benefit yang masih aktif
        // Record EmployeeBenefit tetap dipertahankan agar integritas histori Claims tidak rusak.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeBenefits = await db.EmployeeBenefits
            .Where(eb => eb.EmployeeId == command.Id && eb.EndDate >= today)
            .ToListAsync(ct);

        foreach (var eb in activeBenefits)
        {
            eb.EndDate = today.AddDays(-1);
        }

        // 4. Hapus Account dan AccountRoles (Akses login dihapus permanen, tetapi data Employee tetap ada di riwayat)
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.EmployeeId == command.Id, ct);

        if (account != null)
        {
            var accountRoles = await db.AccountRoles
                .Where(ar => ar.AccountId == account.Id)
                .ToListAsync(ct);

            if (accountRoles.Count > 0)
            {
                db.AccountRoles.RemoveRange(accountRoles);
            }

            db.Accounts.Remove(account);
        }

        await db.SaveChangesAsync(ct);
    }
}
