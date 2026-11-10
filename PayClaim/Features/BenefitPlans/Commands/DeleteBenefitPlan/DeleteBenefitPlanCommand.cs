using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Features.BenefitPlans.Commands;

public record DeleteBenefitPlanCommand(string DepartmentId, Guid BenefitId, string PositionId) : IRequest;

public class DeleteBenefitPlanHandler(IUnitOfWork uow) : IRequestHandler<DeleteBenefitPlanCommand>
{
    public async Task Handle(DeleteBenefitPlanCommand command, CancellationToken ct)
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

        uow.BenefitPlans.Delete(benefitPlan);

        await uow.SaveChangesAsync(ct);
    }
}
