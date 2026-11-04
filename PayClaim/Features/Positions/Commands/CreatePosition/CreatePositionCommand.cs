using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Master;
using PayClaim.Entities;

namespace PayClaim.Features.Positions.Commands.CreatePosition;

public record CreatePositionCommand(CreatePositionDto Request);

public class CreatePositionHandler(IUnitOfWork uow)
{
    public async Task<string> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var existingPosition = await uow.Positions.GetByIdAsync(
            new object[] { request.Id }, ct);

        if (existingPosition != null)
        {
            throw new ConflictException("Position ID already exists.");
        }

        var position = new Position
        {
            Id = request.Id,
            Title = request.Title
        };

        await uow.Positions.AddAsync(position, ct);

        await uow.SaveChangesAsync(ct);

        return position.Id;
    }
}