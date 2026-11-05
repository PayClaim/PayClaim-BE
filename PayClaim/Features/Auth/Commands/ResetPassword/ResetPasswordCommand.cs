using BCrypt.Net;
using PayClaim.Common.Exceptions;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Auth;
using PayClaim.Entities;

namespace PayClaim.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequestDto Request);

public class ResetPasswordHandler(IUnitOfWork uow)
{
    public async Task Handle(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        Account? account = null;

        if (request.EmployeeId.HasValue)
        {
            account = await uow.Accounts.GetByEmployeeIdWithRolesAsync(request.EmployeeId.Value, ct);
        }
        else
        {
            var identifier = request.GetIdentifier();
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                account = await uow.Accounts.GetByIdentifierAsync(identifier, ct);
            }
        }

        if (account == null)
        {
            throw new NotFoundException("Account not found with the provided identifier.");
        }

        if (!account.IsActive)
        {
            throw new BusinessRuleException("Account is inactive. Please contact your administrator.");
        }

        // Hash new password using BCrypt
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        uow.Accounts.Update(account);
        await uow.SaveChangesAsync(ct);
    }
}
