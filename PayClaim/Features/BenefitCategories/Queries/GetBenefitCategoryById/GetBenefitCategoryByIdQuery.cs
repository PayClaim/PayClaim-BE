using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.BenefitCategories.Queries.GetBenefitCategoryById;

public record GetBenefitCategoryByIdQuery(int Id) : IRequest<BenefitCategoryDto>;

public class GetBenefitCategoryByIdQueryHandler(PayClaimDbContext db) : IRequestHandler<GetBenefitCategoryByIdQuery, BenefitCategoryDto>
{
    public async Task<BenefitCategoryDto> Handle(GetBenefitCategoryByIdQuery query, CancellationToken ct)
    {
        var category = await db.BenefitCategories
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new BenefitCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .FirstOrDefaultAsync(ct);

        if (category == null)
        {
            throw new NotFoundException(
                "Benefit category not found.");
        }

        return category;
    }
}
