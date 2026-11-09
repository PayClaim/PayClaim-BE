using BCrypt.Net;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Auth;
using PayClaim.Entities;

namespace PayClaim.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(ResetPasswordRequestDto Request);

public class ResetPasswordHandler(IUnitOfWork uow, IEmailService emailService)
{
    public async Task Handle(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var identifier = request.Username.Trim();

        var account = await uow.Accounts.GetByIdentifierAsync(identifier, ct);

        if (account == null)
        {
            throw new NotFoundException("Account not found with the provided username or email.");
        }

        if (!account.IsActive)
        {
            throw new BusinessRuleException("Account is inactive. Please contact your administrator.");
        }

        // Hash new password using BCrypt
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        uow.Accounts.Update(account);
        await uow.SaveChangesAsync(ct);

        // Kirim email notifikasi reset kata sandi ke karyawan
        var employee = account.Employee ?? await uow.Employees.GetByIdAsync(new object[] { account.EmployeeId }, ct);
        if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
        {
            await emailService.SendPasswordResetEmailAsync(
                employee.Email,
                employee.FullName,
                account.Username,
                request.NewPassword,
                ct);
        }
    }
}
