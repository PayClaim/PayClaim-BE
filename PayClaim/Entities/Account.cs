namespace PayClaim.Entities;

public class Account
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public ICollection<AccountRole> AccountRoles { get; set; }
        = new List<AccountRole>();
}