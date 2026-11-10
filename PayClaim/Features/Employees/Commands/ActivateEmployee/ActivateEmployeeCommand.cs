using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.Entities;

namespace PayClaim.Features.Employees.Commands.ActivateEmployee;

public record ActivateEmployeeCommand(Guid Id) : IRequest;

public class ActivateEmployeeHandler(PayClaimDbContext db) : IRequestHandler<ActivateEmployeeCommand>
{
    public async Task Handle(ActivateEmployeeCommand command, CancellationToken ct)
    {
        var employee = await db.Employees
            .Include(e => e.Position)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == command.Id, ct);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }

        if (employee.IsActive)
        {
            return; // Already active
        }

        // 1. Ubah status employee menjadi aktif
        employee.IsActive = true;

        // 2. Buat ulang Account & AccountRoles jika belum ada
        var account = await db.Accounts
            .Include(a => a.AccountRoles)
            .FirstOrDefaultAsync(a => a.EmployeeId == command.Id, ct);

        if (account == null)
        {
            var baseUsername = string.Concat(employee.FullName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(baseUsername))
            {
                baseUsername = $"user{employee.Number.Replace("-", "").ToLowerInvariant()}";
            }
            var username = baseUsername;
            var suffix = 1;

            while (await db.Accounts.AnyAsync(a => a.Username == username, ct))
            {
                username = $"{baseUsername}{suffix:D2}";
                suffix++;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

            account = new Account
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = passwordHash,
                IsActive = true,
                EmployeeId = employee.Id
            };

            await db.Accounts.AddAsync(account, ct);
        }
        else
        {
            account.IsActive = true;
        }

        // 3. Pastikan role sesuai posisi & departemen
        var targetRoleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Employee" };

        if (employee.PositionId == "MGR")
        {
            if (employee.DepartmentId == "FIN")
            {
                targetRoleNames.Add("Finance Manager");
                targetRoleNames.Add("Finance");
            }
            else
            {
                targetRoleNames.Add("Manager");
            }
        }
        else if (employee.DepartmentId == "FIN")
        {
            targetRoleNames.Add("Finance");
        }

        var existingRoles = await db.AccountRoles
            .Where(ar => ar.AccountId == account.Id)
            .Include(ar => ar.Role)
            .ToListAsync(ct);

        foreach (var roleName in targetRoleNames)
        {
            if (!existingRoles.Any(er => er.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
                if (role != null)
                {
                    await db.AccountRoles.AddAsync(new AccountRole
                    {
                        AccountId = account.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    }, ct);
                }
            }
        }

        // 4. Pulihkan jatah EmployeeBenefit tahun ini sesuai BenefitPlans
        var today = DateOnly.FromDateTime(DateTime.Today);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        var hasActiveBenefits = await db.EmployeeBenefits
            .AnyAsync(eb => eb.EmployeeId == employee.Id && eb.EndDate >= today, ct);

        if (!hasActiveBenefits)
        {
            var matchingPlans = await db.BenefitPlans
                .Where(bp => (bp.DepartmentId == employee.DepartmentId || bp.DepartmentId == "ALL") &&
                             bp.PositionId == employee.PositionId)
                .ToListAsync(ct);

            foreach (var plan in matchingPlans)
            {
                var existingBenefit = await db.EmployeeBenefits
                    .FirstOrDefaultAsync(eb => eb.EmployeeId == employee.Id && 
                                              eb.BenefitId == plan.BenefitId &&
                                              eb.StartDate.Year == today.Year, ct);

                if (existingBenefit != null)
                {
                    existingBenefit.EndDate = endOfYear;
                }
                else
                {
                    await db.EmployeeBenefits.AddAsync(new EmployeeBenefit
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        BenefitId = plan.BenefitId,
                        Balance = plan.LimitAmount,
                        StartDate = today,
                        EndDate = endOfYear
                    }, ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
