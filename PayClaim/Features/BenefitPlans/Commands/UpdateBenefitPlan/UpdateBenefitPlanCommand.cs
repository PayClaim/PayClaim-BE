using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.BenefitPlans.Commands;

public record UpdateBenefitPlanCommand(
    string DepartmentId,
    Guid BenefitId,
    string PositionId,
    UpsertBenefitPlanDto Request) : IRequest;

public class UpdateBenefitPlanHandler(IUnitOfWork uow) : IRequestHandler<UpdateBenefitPlanCommand>
{
    public async Task Handle(
        UpdateBenefitPlanCommand command,
        CancellationToken ct)
    {
        var benefitPlan = await uow.BenefitPlans.GetByIdAsync(
            new object[]
            {
                command.DepartmentId,
                command.BenefitId,
                command.PositionId
            }, ct);

        if (benefitPlan == null)
            throw new NotFoundException("Benefit plan not found.");

        var request = command.Request;

        if (string.Equals(request.DepartmentId, "ALL", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("Cannot update a single benefit plan with DepartmentId 'ALL'.");

        // Validate Department
        var department = await uow.Departments.GetByIdAsync(
            new object[] { request.DepartmentId }, ct);

        if (department == null)
            throw new NotFoundException("Department not found.");

        // Validate Position
        var position = await uow.Positions.GetByIdAsync(
            new object[] { request.PositionId }, ct);

        if (position == null)
            throw new NotFoundException("Position not found.");

        // Validate Benefit
        var benefit = await uow.Benefits.GetByIdAsync(
            new object[] { request.BenefitId }, ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        if (request.LimitAmount < 0)
            throw new BusinessRuleException("Limit amount cannot be negative.");

        bool keyChanged =
            !string.Equals(request.DepartmentId, command.DepartmentId, StringComparison.Ordinal) ||
            request.BenefitId != command.BenefitId ||
            !string.Equals(request.PositionId, command.PositionId, StringComparison.Ordinal);

        if (!keyChanged)
        {
            benefitPlan.LimitAmount = request.LimitAmount;
            uow.BenefitPlans.Update(benefitPlan);
        }
        else
        {
            // Check whether the new composite key combination already exists
            var existingPlan = await uow.BenefitPlans.GetByIdAsync(
                new object[]
                {
                    request.DepartmentId,
                    request.BenefitId,
                    request.PositionId
                }, ct);

            if (existingPlan != null)
            {
                throw new ConflictException("Benefit plan already exists.");
            }

            // EF Core does not allow modifying composite primary key properties on a tracked entity.
            // Delete the existing record and insert a new one with the updated composite key.
            uow.BenefitPlans.Delete(benefitPlan);

            var newBenefitPlan = new BenefitPlan
            {
                DepartmentId = request.DepartmentId,
                BenefitId = request.BenefitId,
                PositionId = request.PositionId,
                LimitAmount = request.LimitAmount
            };

            await uow.BenefitPlans.AddAsync(newBenefitPlan, ct);
        }

        await uow.SaveChangesAsync(ct);
    }
}
