using BCrypt.Net;
using MediatR;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.Auth;

namespace PayClaim.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequestDto Request) : IRequest<LoginResponseDto>;

public class LoginHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IConfiguration configuration) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var account = await unitOfWork.Accounts.GetByUsernameWithDetailsAsync(request.Username, ct);

        if (account == null)
        {
            throw new BusinessRuleException("Invalid username or password.");
        }

        if (!account.IsActive)
        {
            throw new BusinessRuleException("Account is inactive. Please contact your administrator.");
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
}
