using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.Master;

public class DepartmentDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ParentDeptId { get; set; }

    public string? ParentDeptName { get; set; }
}

public class CreateDepartmentDto
{
    [Required(ErrorMessage = "Department ID is required.")]
    [StringLength(50, ErrorMessage = "Department ID cannot exceed 50 characters.")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Parent department ID cannot exceed 50 characters.")]
    public string? ParentDeptId { get; set; }
}

public class UpdateDepartmentDto
{
    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Parent department ID cannot exceed 50 characters.")]
    public string? ParentDeptId { get; set; }
}
