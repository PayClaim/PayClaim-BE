using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;

namespace PayClaim.Features.Positions.Commands.DeletePosition;

public record DeletePositionCommand(string Id);

public class DeletePositionHandler(IUnitOfWork uow)
{
    public async Task Handle(DeletePositionCommand command, CancellationToken ct)
    {
        var position = await uow.Positions.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (position == null)
        {
            throw new NotFoundException("Position not found.");
        }

        uow.Positions.Delete(position);

        await uow.SaveChangesAsync(ct);
    }
}