using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.Master.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(CreateDepartmentDto Request) : IRequest<string>;

public class CreateDepartmentHandler(IUnitOfWork uow) : IRequestHandler<CreateDepartmentCommand, string>
{
    public async Task<string> Handle(CreateDepartmentCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (await uow.Departments.GetByIdAsync(
            new object[] { request.Id }, ct) != null)
        {
            throw new ConflictException("Department ID already exists.");
        }

        if (!string.IsNullOrEmpty(request.ParentDeptId))
        {
            var parentDepartment = await uow.Departments.GetByIdAsync(
                new object[] { request.ParentDeptId }, ct);

            if (parentDepartment == null)
            {
                throw new NotFoundException("Parent department not found.");
            }
        }

        var department = new Department
        {
            Id = request.Id,
            Name = request.Name,
            ParentDepartmentId = request.ParentDeptId
        };

        await uow.Departments.AddAsync(department, ct);
        await uow.SaveChangesAsync(ct);

        return department.Id;
    }
}
