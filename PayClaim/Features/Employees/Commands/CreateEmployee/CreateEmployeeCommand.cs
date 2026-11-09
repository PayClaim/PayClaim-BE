using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;
using PayClaim.Enums;
using System.Security.Cryptography;

namespace PayClaim.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(CreateEmployeeDto Request);

public class CreateEmployeeHandler(IUnitOfWork uow, PayClaim.Common.Services.IEmailService emailService)
{
    public async Task<Guid> Handle(
        CreateEmployeeCommand command,
        CancellationToken ct)
    {
        var request = command.Request;

        var year = DateTime.UtcNow.Year;
        string employeeNumber;

        do
        {
            var randomNumber = RandomNumberGenerator.GetInt32(0, 10000);
            employeeNumber = $"EMP-{year}-{randomNumber:D4}";
        }
        while (await uow.Employees.ExistsByNumberAsync(employeeNumber, null, ct));

        if (await uow.Employees.ExistsByEmailAsync(request.Email, null, ct))
        {
            throw new ConflictException("Employee email already exists.");
        }

        var baseUsername = string.Concat(request.FullName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        var username = baseUsername;
        var suffix = 1;

        while (await uow.Accounts.ExistsByUsernameAsync(username, ct))
        {
            username = $"{baseUsername}{suffix:D2}";
            suffix++;
        }

        var department = await uow.Departments.GetByIdAsync(new object[] { request.DepartmentId }, ct);

        if (department == null)
        {
            throw new NotFoundException("Department not found.");
        }

        var position = await uow.Positions.GetByIdAsync(new object[] { request.PositionId }, ct);
        if (position == null)
        {
            throw new NotFoundException("Position not found.");
        }

        if (request.ManagerId.HasValue)
        {
            var manager = await uow.Employees.GetByIdAsync(new object[] { request.ManagerId.Value }, ct);

            if (manager == null)
            {
                throw new NotFoundException("Manager not found.");
            }
        }

        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Employee" };

        if (request.PositionId == "MGR")
        {
            if (request.DepartmentId == "FIN")
            {
                roleNames.Add("Finance Manager");
                roleNames.Add("Finance");
            }
            else
            {
                roleNames.Add("Manager");
            }
        }
        else if (request.DepartmentId == "FIN")
        {
            roleNames.Add("Finance");
        }

        var roleIds = new List<Guid>();

        foreach (var name in roleNames)
        {
            var role = await uow.Roles.GetByNameAsync(name, ct);

            if (role != null)
            {
                roleIds.Add(role.Id);
            }
        }

        var benefitPlans = await uow.BenefitPlans.GetByDepartmentAndPositionAsync(request.DepartmentId, request.PositionId, ct);

        var employeeBenefits = new List<EmployeeBenefit>();

        foreach (var plan in benefitPlans)
        {
            var benefit = await uow.Benefits.GetByIdAsync(new object[] { plan.BenefitId }, ct);

            if (benefit == null)
            {
                throw new NotFoundException(
                    $"Benefit with ID {plan.BenefitId} was not found.");
            }

            DateOnly endDate;

            switch (benefit.PeriodType)
            {
                case BenefitPeriodType.Monthly:
                    endDate = request.HireDate
                        .AddMonths(1)
                        .AddDays(-1);
                    break;

                case BenefitPeriodType.Yearly:
                    endDate = request.HireDate
                        .AddYears(1)
                        .AddDays(-1);
                    break;

                case BenefitPeriodType.OneTime:
                    endDate = request.HireDate
                        .AddYears(1)
                        .AddDays(-1);
                    break;

                default:
                    throw new BusinessRuleException("Invalid benefit period type.");
            }

            employeeBenefits.Add(new EmployeeBenefit
            {
                Id = Guid.CreateVersion7(),
                EmployeeId = Guid.Empty, // Set after Employee is created
                BenefitId = plan.BenefitId,
                Balance = plan.LimitAmount,
                StartDate = request.HireDate,
                EndDate = endDate
            });
        }

        var employee = new Employee
        {
            Id = Guid.CreateVersion7(),
            Number = employeeNumber,
            FullName = request.FullName,
            Email = request.Email,
            HireDate = request.HireDate,
            IsActive = true,
            PositionId = request.PositionId,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId
        };

        foreach (var employeeBenefit in employeeBenefits)
        {
            employeeBenefit.EmployeeId = employee.Id;
        }

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employee.Id,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            IsActive = true
        };

        var accountRoles = new List<AccountRole>();

        foreach (var roleId in roleIds)
        {
            accountRoles.Add(new AccountRole
            {
                AccountId = account.Id,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
        }

        await uow.Employees.AddAsync(employee, ct);
        await uow.Accounts.AddAsync(account, ct);

        foreach (var accountRole in accountRoles)
        {
            await uow.AccountRoles.AddAsync(accountRole, ct);
        }

        foreach (var employeeBenefit in employeeBenefits)
        {
            await uow.EmployeeBenefits.AddAsync(employeeBenefit, ct);
        }

        await uow.SaveChangesAsync(ct);

        await emailService.SendWelcomeEmployeeEmailAsync(
            employee.Email,
            employee.FullName,
            employee.Number,
            account.Username,
            "Password123!",
            department.Name,
            position.Title,
            ct);

        return employee.Id;
    }
}
