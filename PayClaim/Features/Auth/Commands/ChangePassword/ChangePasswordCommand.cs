using BCrypt.Net;
using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Auth;

namespace PayClaim.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(ChangePasswordRequestDto Request, Guid? AccountId = null) : IRequest;

public class ChangePasswordHandler(IUnitOfWork uow, ICurrentUser currentUser) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand command, CancellationToken ct = default)
    {
        var targetAccountId = command.AccountId ?? currentUser.RequiredAccountId;
        var request = command.Request;

        var account = await uow.Accounts.GetByIdAsync(new object[] { targetAccountId }, ct);
        if (account == null)
        {
            throw new NotFoundException("Account not found.");
        }

        if (!account.IsActive)
        {
            throw new BusinessRuleException("Account is inactive. Please contact your administrator.");
        }

        // Validate old/current password with BCrypt
        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            throw new BusinessRuleException("Current password is incorrect.");
        }

        // New password should not be identical to current password
        if (request.CurrentPassword == request.NewPassword)
        {
            throw new BusinessRuleException("New password cannot be the same as the current password.");
        }

        // Hash new password using BCrypt
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        uow.Accounts.Update(account);
        await uow.SaveChangesAsync(ct);
    }
}
