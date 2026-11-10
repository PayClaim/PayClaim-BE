using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitCategories.Commands.UpdateBenefitCategory;

public record UpdateBenefitCategoryCommand(
    int Id,
    CreateBenefitCategoryDto Request
) : IRequest;

public class UpdateBenefitCategoryHandler(IUnitOfWork uow) : IRequestHandler<UpdateBenefitCategoryCommand>
{
    public async Task Handle(UpdateBenefitCategoryCommand command, CancellationToken ct)
    {
        var category = await uow.BenefitCategories.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (category == null)
        {
            throw new NotFoundException(
                "Benefit category not found.");
        }

        var request = command.Request;

        category.Name = request.Name;

        uow.BenefitCategories.Update(category);

        await uow.SaveChangesAsync(ct);
    }
}
