using PayClaim.Entities;

namespace PayClaim.Common.Security;

public interface IJwtTokenService
{
    string GenerateToken(Account account);
}
