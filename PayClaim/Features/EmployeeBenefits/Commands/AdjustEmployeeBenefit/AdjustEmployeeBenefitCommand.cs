using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.EmployeeBenefit;

namespace PayClaim.Features.EmployeeBenefits.Commands;

public record AdjustEmployeeBenefitCommand(
    Guid EmployeeBenefitId,
    AdjustEmployeeBenefitDto Request);

public class AdjustEmployeeBenefitHandler(IUnitOfWork uow)
{
    public async Task Handle(
        AdjustEmployeeBenefitCommand command,
        CancellationToken ct)
    {
        var employeeBenefit = await uow.EmployeeBenefits.GetByIdAsync(
            new object[] { command.EmployeeBenefitId }, ct);

        if (employeeBenefit == null)
            throw new NotFoundException("Employee benefit not found.");

        if (command.Request.AdjustmentAmount == 0)
            throw new BusinessRuleException("Adjustment amount cannot be zero.");

        var newBalance = employeeBenefit.Balance
            + command.Request.AdjustmentAmount;

        if (newBalance < 0)
            throw new BusinessRuleException("Balance cannot be negative.");

        employeeBenefit.Balance = newBalance;

        uow.EmployeeBenefits.Update(employeeBenefit);

        await uow.SaveChangesAsync(ct);
    }
}