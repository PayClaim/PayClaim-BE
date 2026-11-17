using MediatR;
using Microsoft.Extensions.Caching.Memory;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.Features.Departments.Queries.GetDepartment;

namespace PayClaim.Features.Master.Departments.Commands.DeleteDepartment;

public record DeleteDepartmentCommand(string Id) : IRequest;

public class DeleteDepartmentHandler(IUnitOfWork uow, IMemoryCache cache) : IRequestHandler<DeleteDepartmentCommand>
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
        cache.Remove(GetDepartmentsHandler.CacheKey);
    }
}
