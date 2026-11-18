using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Auth;

namespace PayClaim.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequestDto Request) : IRequest<LoginResponseDto>;

public class LoginHandler(
    IUnitOfWork unitOfWork,
    PayClaimDbContext context,
    IJwtTokenService jwtTokenService,
    IConfiguration configuration) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var username = request.Username.Trim();

        var account = await unitOfWork.Accounts.GetByUsernameWithDetailsAsync(username, ct);

        if (account == null)
        {
            // Fallback untuk employee yang terlanjur dinonaktifkan saat DeleteEmployee masih menghapus row Account
            if (await IsInactiveEmployeeWithoutAccountAsync(username, ct))
            {
                throw new BusinessRuleException("User is not active!");
            }

            throw new BusinessRuleException("Invalid username or password.");
        }

        if (!account.IsActive || account.Employee is { IsActive: false })
        {
            throw new BusinessRuleException("User is not active!");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);
        if (!isPasswordValid)
        {
            throw new BusinessRuleException("Invalid username or password.");
        }

        var token = jwtTokenService.GenerateToken(account);

        var expiryDays = double.TryParse(configuration["Jwt:ExpiryDays"], out var days) ? days : 1;
        var expiresAt = DateTime.UtcNow.AddDays(expiryDays);

        var userProfile = new UserProfileResponseDto
        {
            AccountId = account.Id,
            EmployeeId = account.EmployeeId,
            EmployeeNumber = account.Employee?.Number ?? string.Empty,
            Username = account.Username,
            FullName = account.Employee?.FullName ?? string.Empty,
            Email = account.Employee?.Email ?? string.Empty,
            DepartmentId = account.Employee?.DepartmentId ?? string.Empty,
            DepartmentName = account.Employee?.Department?.Name,
            PositionId = account.Employee?.PositionId ?? string.Empty,
            PositionTitle = account.Employee?.Position?.Title,
            ManagerId = account.Employee?.ManagerId,
            ManagerName = account.Employee?.Manager?.FullName,
            Roles = account.AccountRoles
                .Where(ar => ar.Role != null)
                .Select(ar => ar.Role.Name)
                .ToList()
        };

        return new LoginResponseDto
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = userProfile
        };
    }

    private async Task<bool> IsInactiveEmployeeWithoutAccountAsync(string username, CancellationToken ct)
    {
        var normalizedInput = username.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        var inactiveEmployees = await context.Employees
            .AsNoTracking()
            .Where(e => !e.IsActive && !context.Accounts.Any(a => a.EmployeeId == e.Id))
            .Select(e => new { e.FullName, e.Email, e.Number })
            .ToListAsync(ct);

        return inactiveEmployees.Any(e =>
        {
            var baseFromName = string.Concat(e.FullName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
            var baseFromNumber = $"user{e.Number.Replace("-", "").ToLowerInvariant()}";
            var emailLocalPart = e.Email.Split('@')[0].ToLowerInvariant();

            return normalizedInput == emailLocalPart ||
                   normalizedInput == baseFromNumber ||
                   (!string.IsNullOrEmpty(baseFromName) &&
                    (normalizedInput == baseFromName ||
                     (normalizedInput.StartsWith(baseFromName, StringComparison.Ordinal) &&
                      normalizedInput[baseFromName.Length..].All(char.IsDigit))));
        });
    }
}
