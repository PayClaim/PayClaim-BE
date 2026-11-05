using System.ComponentModel.DataAnnotations;
using PayClaim.Enums;

namespace PayClaim.DTOs.Master;

public class BenefitCategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CreateBenefitCategoryDto
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}

public class BenefitDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public BenefitPeriodType PeriodType { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }
}

public class CreateBenefitDto
{
    [Required(ErrorMessage = "Benefit name is required.")]
    [StringLength(100, ErrorMessage = "Benefit name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Period type is required.")]
    public BenefitPeriodType PeriodType { get; set; }

    [Required(ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }
}

public class UpdateBenefitDto
{
    [Required(ErrorMessage = "Benefit name is required.")]
    [StringLength(100, ErrorMessage = "Benefit name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Period type is required.")]
    public BenefitPeriodType PeriodType { get; set; }

    [Required(ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }
}

public class BenefitIdResponseDto
{
    public Guid Id { get; set; }

    public BenefitIdResponseDto() { }

    public BenefitIdResponseDto(Guid id)
    {
        Id = id;
    }
}

public class BenefitMessageResponseDto
{
    public string Message { get; set; } = string.Empty;

    public BenefitMessageResponseDto() { }

    public BenefitMessageResponseDto(string message)
    {
        Message = message;
    }
}
