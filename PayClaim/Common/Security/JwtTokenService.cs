using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PayClaim.Entities;
using UserClaim = System.Security.Claims.Claim;

namespace PayClaim.Common.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Account account)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<UserClaim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new("AccountId", account.Id.ToString()),
            new("EmployeeId", account.EmployeeId.ToString())
        };

        if (account.Employee != null)
        {
            if (!string.IsNullOrEmpty(account.Employee.Email))
            {
                claims.Add(new UserClaim(ClaimTypes.Email, account.Employee.Email));
            }

            if (!string.IsNullOrEmpty(account.Employee.FullName))
            {
                claims.Add(new UserClaim("FullName", account.Employee.FullName));
            }

            if (!string.IsNullOrEmpty(account.Employee.DepartmentId))
            {
                claims.Add(new UserClaim("DepartmentId", account.Employee.DepartmentId));
            }

            if (!string.IsNullOrEmpty(account.Employee.PositionId))
            {
                claims.Add(new UserClaim("PositionId", account.Employee.PositionId));
            }
        }

        if (account.AccountRoles != null)
        {
            foreach (var accountRole in account.AccountRoles)
            {
                if (accountRole.Role != null && !string.IsNullOrEmpty(accountRole.Role.Name))
                {
                    claims.Add(new UserClaim(ClaimTypes.Role, accountRole.Role.Name));
                }
            }
        }

        var expiryDays = double.TryParse(jwtSettings["ExpiryDays"], out var days) ? days : 1;

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
