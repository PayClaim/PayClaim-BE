using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Entities;
using PayClaim.Enums;
using PayClaim.Features.EmployeeBenefits;

namespace PayClaim.Features.Employees.Commands.ActivateEmployee;

public record ActivateEmployeeCommand(Guid Id) : IRequest;

public class ActivateEmployeeHandler(
    PayClaimDbContext db,
    IEmailService emailService) : IRequestHandler<ActivateEmployeeCommand>
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

        var isNewAccount = account == null;
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
                        AssignedAt = AppTime.WibNow
                    }, ct);
                }
            }
        }

        // 4. Pulihkan jatah EmployeeBenefit sesuai BenefitPlans dan PeriodType masing-masing
        var today = AppTime.WibToday;

        var matchingPlans = await db.BenefitPlans
            .Include(bp => bp.Benefit)
            .Where(bp => (bp.DepartmentId == employee.DepartmentId || bp.DepartmentId == "ALL") &&
                         bp.PositionId == employee.PositionId)
            .OrderBy(bp => bp.DepartmentId == "ALL" ? 1 : 0)
            .ToListAsync(ct);

        var existingBenefits = await db.EmployeeBenefits
            .Where(eb => eb.EmployeeId == employee.Id)
            .ToListAsync(ct);

        var processedBenefitIds = new HashSet<Guid>();
        foreach (var plan in matchingPlans)
        {
            if (!processedBenefitIds.Add(plan.BenefitId))
            {
                continue;
            }

            var (periodStart, periodEnd) = BenefitPeriodHelper.GetCurrentPeriod(
                plan.Benefit.PeriodType,
                today);

            var existingBenefit = existingBenefits
                .FirstOrDefault(eb => eb.BenefitId == plan.BenefitId);

            if (existingBenefit != null)
            {
                if (plan.Benefit.PeriodType != BenefitPeriodType.OneTime &&
                    existingBenefit.StartDate < periodStart)
                {
                    existingBenefit.StartDate = periodStart;
                    existingBenefit.EndDate = periodEnd;
                    existingBenefit.Balance = plan.LimitAmount;
                }
                else
                {
                    existingBenefit.EndDate = periodEnd;
                }
            }
            else
            {
                await db.EmployeeBenefits.AddAsync(new EmployeeBenefit
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    BenefitId = plan.BenefitId,
                    Balance = plan.LimitAmount,
                    StartDate = periodStart,
                    EndDate = periodEnd
                }, ct);
            }
        }

        await db.SaveChangesAsync(ct);

        if (isNewAccount && !string.IsNullOrWhiteSpace(employee.Email))
        {
            await emailService.SendWelcomeEmployeeEmailAsync(
                employee.Email,
                employee.FullName,
                employee.Number,
                account.Username,
                "Password123!",
                employee.Department?.Name ?? employee.DepartmentId,
                employee.Position?.Title ?? employee.PositionId,
                ct);
        }
    }
}
