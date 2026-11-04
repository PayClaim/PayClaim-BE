using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Benefits.Commands;

public record UpdateBenefitCommand(Guid Id, UpdateBenefitDto Request);

public class UpdateBenefitHandler(IUnitOfWork uow)
{
    public async Task Handle(
        UpdateBenefitCommand command,
        CancellationToken ct)
    {
        var benefit = await uow.Benefits.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        var request = command.Request;

        // Validate period type
        if (!Enum.IsDefined(request.PeriodType))
            throw new BusinessRuleException("Invalid benefit period type.");

        // Validate category
        var category = await uow.BenefitCategories.GetByIdAsync(
            new object[] { request.CategoryId }, ct);

        if (category == null)
            throw new NotFoundException("Benefit category not found.");

        benefit.Name = request.Name;
        benefit.Description = request.Description;
        benefit.PeriodType = request.PeriodType;
        benefit.CategoryId = request.CategoryId;

        uow.Benefits.Update(benefit);

        await uow.SaveChangesAsync(ct);
    }
}