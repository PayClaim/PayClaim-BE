using System.ComponentModel.DataAnnotations;
using PayClaim.DTOs.Common;

namespace PayClaim.DTOs.Master;

public class EmployeeDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; }

    public string PositionId { get; set; } = string.Empty;

    public string? PositionTitle { get; set; }

    public string DepartmentId { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public Guid? ManagerId { get; set; }

    public string? ManagerName { get; set; }
}

public class CreateEmployeeDto
{
    //[Required(ErrorMessage = "Employee number is required.")]
    //[StringLength(20, ErrorMessage = "Employee number cannot exceed 20 characters.")]
    //public string Number { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateOnly HireDate { get; set; }


    [Required(ErrorMessage = "Position ID is required.")]
    [StringLength(50, ErrorMessage = "Position ID cannot exceed 50 characters.")]
    public string PositionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department ID is required.")]
    [StringLength(50, ErrorMessage = "Department ID cannot exceed 50 characters.")]
    public string DepartmentId { get; set; } = string.Empty;

    public Guid? ManagerId { get; set; }

    //// Initial Account Information
    //[Required(ErrorMessage = "Username is required for the user account.")]
    //[StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    //public string Username { get; set; } = string.Empty;

    //[Required(ErrorMessage = "Password is required for the user account.")]
    //[MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    //public string Password { get; set; } = string.Empty;

    //[Required(ErrorMessage = "At least one role ID is required.")]
    //[MinLength(1, ErrorMessage = "At least one role ID is required.")]
    //public List<Guid> RoleIds { get; set; } = new();
}

public class UpdateEmployeeDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateOnly HireDate { get; set; }

    [Required(ErrorMessage = "Position ID is required.")]
    public string PositionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department ID is required.")]
    public string DepartmentId { get; set; } = string.Empty;

    public Guid? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmployeeFilterQuery
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Search { get; set; }

    public string? DepartmentId { get; set; }

    public string? PositionId { get; set; }

    public bool? IsActive { get; set; }
}
