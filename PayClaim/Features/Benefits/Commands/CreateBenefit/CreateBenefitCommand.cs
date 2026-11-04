using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;
using PayClaim.Common.Exceptions;

namespace PayClaim.Features.Benefits.Commands;

public record CreateBenefitCommand(CreateBenefitDto Request);

public class CreateBenefitHandler(IUnitOfWork uow)
{
    public async Task<Guid> Handle(CreateBenefitCommand command, CancellationToken ct)
    {
        var request = command.Request;

        // Validate period type
        if (!Enum.IsDefined(request.PeriodType))
            throw new BusinessRuleException("Invalid benefit period type.");

        // Validate category
        var category = await uow.BenefitCategories.GetByIdAsync(
            new object[] { request.CategoryId }, ct);

        if (category == null)
            throw new NotFoundException("Benefit category not found.");

        var benefit = new Benefit
        {
            Name = request.Name,
            Description = request.Description,
            PeriodType = request.PeriodType,
            CategoryId = request.CategoryId
        };

        await uow.Benefits.AddAsync(benefit, ct);
        await uow.SaveChangesAsync(ct);

        return benefit.Id;
    }
}