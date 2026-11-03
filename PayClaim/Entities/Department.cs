namespace PayClaim.Entities;

public class Department
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }
}