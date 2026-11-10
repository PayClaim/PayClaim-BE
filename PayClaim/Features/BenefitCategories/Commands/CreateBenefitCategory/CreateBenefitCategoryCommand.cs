using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.BenefitCategories.Commands.CreateBenefitCategory;

public record CreateBenefitCategoryCommand(
    CreateBenefitCategoryDto Request
) : IRequest<int>;

public class CreateBenefitCategoryHandler(IUnitOfWork uow) : IRequestHandler<CreateBenefitCategoryCommand, int>
{
    public async Task<int> Handle(CreateBenefitCategoryCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var category = new BenefitCategory
        {
            Name = request.Name
        };

        await uow.BenefitCategories.AddAsync(category, ct);

        await uow.SaveChangesAsync(ct);

        return category.Id;
    }
}
