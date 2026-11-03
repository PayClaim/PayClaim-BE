
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.Master.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommand
{
    public CreateDepartmentDto Request { get; set; }

    public CreateDepartmentCommand(CreateDepartmentDto request)
    {
        Request = request;
    }
}

public class CreateDepartmentHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepartmentHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle( CreateDepartmentCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var existingDepartment = await _unitOfWork.Departments
            .GetByIdAsync(new object[] { request.Id }, ct);

        if (existingDepartment != null)
        {
            throw new ConflictException("Department ID already exists.");
        }

        if (!string.IsNullOrEmpty(request.ParentDeptId))
        {
            var parentDepartment = await _unitOfWork.Departments
                .GetByIdAsync(new object[] { request.ParentDeptId }, ct);

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

        await _unitOfWork.Departments.AddAsync(department, ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
