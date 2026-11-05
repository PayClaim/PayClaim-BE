using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(Guid Id, UpdateEmployeeDto Request);

public class UpdateEmployeeHandler(IUnitOfWork uow)
{
    public async Task Handle(UpdateEmployeeCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var employee = await uow.Employees.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (employee == null)
            throw new NotFoundException("Employee not found.");

        var emailExists = await uow.Employees.ExistsByEmailAsync(
            request.Email, employee.Id, ct);

        if (emailExists)
            throw new ConflictException("Employee email already exists.");

        var department = await uow.Departments.GetByIdAsync(
            new object[] { request.DepartmentId }, ct);

        if (department == null)
            throw new NotFoundException("Department not found.");

        var position = await uow.Positions.GetByIdAsync(
            new object[] { request.PositionId }, ct);

        if (position == null)
            throw new NotFoundException("Position not found.");

        if (request.ManagerId.HasValue)
        {
            if (request.ManagerId.Value == employee.Id)
                throw new BusinessRuleException(
                    "Employee cannot be their own manager.");

            var manager = await uow.Employees.GetByIdAsync(
                new object[] { request.ManagerId.Value }, ct);

            if (manager == null)
                throw new NotFoundException("Manager not found.");
        }

        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.HireDate = request.HireDate;
        employee.PositionId = request.PositionId;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.IsActive = request.IsActive;

        var account = await uow.Accounts.GetByEmployeeIdWithRolesAsync(
            employee.Id, ct);

        if (account == null)
            throw new NotFoundException("Employee account not found.");

        account.IsActive = request.IsActive;

        string? roleName = null;

        if (request.PositionId == "MGR")
        {
            if (request.DepartmentId == "FIN")
                roleName = "Finance Manager";
            else
                roleName = "Manager";
        }
        else if (request.PositionId == "L1" ||
                 request.PositionId == "L2" ||
                 request.PositionId == "L3" ||
                 request.PositionId == "L4")
        {
            if (request.DepartmentId == "FIN")
                roleName = "Finance";
            else
                roleName = "Employee";
        }

        var automaticRoleNames = new[]
        {
            "Employee",
            "Manager",
            "Finance",
            "Finance Manager"
        };

        foreach (var accountRole in account.AccountRoles.ToList())
        {
            if (automaticRoleNames.Contains(accountRole.Role.Name))
            {
                uow.AccountRoles.Delete(accountRole);
            }
        }

        if (roleName != null)
        {
            var role = await uow.Roles.GetByNameAsync(roleName, ct);

            if (role == null)
                throw new NotFoundException($"Role '{roleName}' was not found.");

            await uow.AccountRoles.AddAsync(new AccountRole
            {
                AccountId = account.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            }, ct);
        }

        uow.Employees.Update(employee);

        await uow.SaveChangesAsync(ct);
    }
}