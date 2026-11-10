using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Features.Benefits.Commands;

public record DeleteBenefitCommand(Guid Id) : IRequest;

public class DeleteBenefitHandler(IUnitOfWork uow) : IRequestHandler<DeleteBenefitCommand>
{
    public async Task Handle(
        DeleteBenefitCommand command,
        CancellationToken ct)
    {
        var benefit = await uow.Benefits.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (benefit == null)
            throw new NotFoundException("Benefit not found.");

        uow.Benefits.Delete(benefit);

        await uow.SaveChangesAsync(ct);
    }
}
