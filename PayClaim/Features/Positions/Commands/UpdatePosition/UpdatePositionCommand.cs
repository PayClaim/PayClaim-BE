using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;

namespace PayClaim.Features.Positions.Commands.UpdatePosition;

public record UpdatePositionCommand(
    string Id,
    UpdatePositionDto Request
);

public class UpdatePositionHandler(IUnitOfWork uow)
{
    public async Task Handle(UpdatePositionCommand command, CancellationToken ct)
    {
        var position = await uow.Positions.GetByIdAsync(
            new object[] { command.Id }, ct);

        if (position == null)
        {
            throw new NotFoundException("Position not found.");
        }

        var request = command.Request;

        position.Title = request.Title;

        uow.Positions.Update(position);

        await uow.SaveChangesAsync(ct);
    }
}