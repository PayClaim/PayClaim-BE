using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Departments.Commands.UpdataDepartment;

public record UpdateDepartmentCommand(string Id, UpdateDepartmentDto Request) : IRequest;

public class UpdateDepartmentHandler(IUnitOfWork uow) : IRequestHandler<UpdateDepartmentCommand>
{
    public async Task Handle(UpdateDepartmentCommand command, CancellationToken ct)
    {
        var department = await uow.Departments.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (department == null)
        {
            throw new NotFoundException("Department not found!");
        }

        var request = command.Request;

        if (!string.IsNullOrEmpty(request.ParentDeptId))
        {
            if (request.ParentDeptId == department.Id)
            {
                throw new BusinessRuleException("Department cannot be its own parent!");
            }

            var parentDepartment = await uow.Departments.GetByIdAsync(
                new object[] { request.ParentDeptId }, ct);

            if (parentDepartment == null)
            {
                throw new NotFoundException("Parent department not found.");
            }
        }
        department.Name = request.Name;
        department.ParentDepartmentId = request.ParentDeptId;

        uow.Departments.Update(department);

        await uow.SaveChangesAsync(ct);
    }
}
