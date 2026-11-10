using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Features.Master.Departments.Commands.DeleteDepartment;

public record DeleteDepartmentCommand(string Id) : IRequest;

public class DeleteDepartmentHandler(IUnitOfWork uow) : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(
        DeleteDepartmentCommand command,
        CancellationToken ct)
    {
        var department = await uow.Departments.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (department == null)
        {
            throw new NotFoundException("Department not found.");
        }

        uow.Departments.Delete(department);

        await uow.SaveChangesAsync(ct);
    }
}
