using System.Security.Claims;

namespace PayClaim.Common.Security;

public interface ICurrentUser
{
    Guid? AccountId { get; }
    Guid? EmployeeId { get; }
    Guid RequiredEmployeeId { get; }
    Guid RequiredAccountId { get; }
    string[] Roles { get; }
    bool IsInRole(string role);
    bool IsAuthenticated { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public Guid? AccountId
    {
        get
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("AccountId")
                ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid RequiredAccountId => AccountId ?? throw new UnauthorizedAccessException("Invalid or missing user identity in token.");

    public Guid? EmployeeId
    {
        get
        {
            var val = User.FindFirstValue("EmployeeId");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public Guid RequiredEmployeeId => EmployeeId ?? throw new UnauthorizedAccessException("Invalid or missing EmployeeId in token.");

    public string[] Roles => User.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool IsInRole(string role) => User.IsInRole(role);
}
