using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;

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

        // Check whether the new combination already exists
        var existingPlan = await uow.BenefitPlans.GetByIdAsync(
            new object[]
            {
                request.DepartmentId,
                request.BenefitId,
                request.PositionId
            }, ct);

        if (existingPlan != null &&
            (request.DepartmentId != command.DepartmentId ||
             request.BenefitId != command.BenefitId ||
             request.PositionId != command.PositionId))
        {
            throw new ConflictException("Benefit plan already exists.");
        }

        benefitPlan.DepartmentId = request.DepartmentId;
        benefitPlan.BenefitId = request.BenefitId;
        benefitPlan.PositionId = request.PositionId;
        benefitPlan.LimitAmount = request.LimitAmount;

        uow.BenefitPlans.Update(benefitPlan);

        await uow.SaveChangesAsync(ct);
    }
}
