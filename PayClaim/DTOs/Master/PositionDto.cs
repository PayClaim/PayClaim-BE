using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.Master;

public class PositionDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}

public class CreatePositionDto
{
    [Required(ErrorMessage = "Position ID is required.")]
    [StringLength(50, ErrorMessage = "Position ID cannot exceed 50 characters.")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position title is required.")]
    [StringLength(100, ErrorMessage = "Position title cannot exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;
}

public class UpdatePositionDto
{
    [Required(ErrorMessage = "Position title is required.")]
    [StringLength(100, ErrorMessage = "Position title cannot exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;
}
