namespace PayClaim.Entities;

public class AccountRole
{
    public Guid RoleId { get; set; }

    public Guid AccountId { get; set; }

    public DateTime AssignedAt { get; set; }

    public Role Role { get; set; } = null!;

    public Account Account { get; set; } = null!;
}