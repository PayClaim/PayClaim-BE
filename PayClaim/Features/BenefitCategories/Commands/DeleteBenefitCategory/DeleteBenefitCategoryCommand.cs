using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Features.BenefitCategories.Commands.DeleteBenefitCategory;

public record DeleteBenefitCategoryCommand(int Id) : IRequest;

public class DeleteBenefitCategoryHandler(IUnitOfWork uow) : IRequestHandler<DeleteBenefitCategoryCommand>
{
    public async Task Handle(DeleteBenefitCategoryCommand command, CancellationToken ct)
    {
        var category = await uow.BenefitCategories.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (category == null)
        {
            throw new NotFoundException(
                "Benefit category not found.");
        }

        uow.BenefitCategories.Delete(category);

        await uow.SaveChangesAsync(ct);
    }
}
