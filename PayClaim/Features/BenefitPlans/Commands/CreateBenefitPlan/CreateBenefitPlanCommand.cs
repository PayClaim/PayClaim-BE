using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.BenefitPlans.Commands;

public record CreateBenefitPlanCommand(UpsertBenefitPlanDto Request);

public class CreateBenefitPlanHandler(IUnitOfWork uow)
{
    public async Task Handle(CreateBenefitPlanCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var department = await uow.Departments.GetByIdAsync(
            new object[] { request.DepartmentId }, ct);

        if (department == null)
            throw new NotFoundException("Department not found.");

        var position = await uow.Positions.GetByIdAsync(
            new object[] { request.PositionId }, ct);

        if (position == null)
            throw new NotFoundException("Position not found.");

        var benefit = await uow.Benefits.GetByIdAsync(
            new object[] { request.BenefitId }, ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        var existingPlan = await uow.BenefitPlans.GetByIdAsync(
            new object[]
            {
                request.DepartmentId,
                request.BenefitId,
                request.PositionId
            }, ct);

        if (existingPlan != null)
            throw new ConflictException("Benefit plan already exists.");

        var benefitPlan = new BenefitPlan
        {
            DepartmentId = request.DepartmentId,
            BenefitId = request.BenefitId,
            PositionId = request.PositionId,
            LimitAmount = request.LimitAmount
        };

        await uow.BenefitPlans.AddAsync(benefitPlan, ct);

        await uow.SaveChangesAsync(ct);
    }
}