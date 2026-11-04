using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Benefits.Queries;

public record GetBenefitByIdQuery(Guid Id);

public class GetBenefitByIdHandler(PayClaimDbContext db)
{
    public async Task<BenefitDto> Handle(
        GetBenefitByIdQuery query,
        CancellationToken ct)
    {
        var benefit = await db.Benefits
            .AsNoTracking()
            .Where(b => b.Id == query.Id)
            .Select(b => new BenefitDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                PeriodType = b.PeriodType,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name
            })
            .FirstOrDefaultAsync(ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        return benefit;
    }
}