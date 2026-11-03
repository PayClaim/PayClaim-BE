namespace PayClaim.DTOs.Auth;

public class UserProfileResponseDto
{
    public Guid AccountId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string PositionId { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public List<string> Roles { get; set; } = new();
}
