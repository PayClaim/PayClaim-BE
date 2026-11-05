using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.Auth;

public class ResetPasswordRequestDto : IValidatableObject
{
    /// <summary>
    /// Username or Email of the account to reset.
    /// </summary>
    public string? EmailOrUsername { get; set; }

    public string? Email { get; set; }

    public string? Username { get; set; }

    public Guid? EmployeeId { get; set; }

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm new password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match new password.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(EmailOrUsername) &&
            string.IsNullOrWhiteSpace(Email) &&
            string.IsNullOrWhiteSpace(Username) &&
            !EmployeeId.HasValue)
        {
            yield return new ValidationResult(
                "Either Email, Username, or EmployeeId must be provided.",
                [nameof(EmailOrUsername), nameof(Email), nameof(Username), nameof(EmployeeId)]);
        }
    }

    public string? GetIdentifier()
    {
        if (!string.IsNullOrWhiteSpace(EmailOrUsername)) return EmailOrUsername.Trim();
        if (!string.IsNullOrWhiteSpace(Username)) return Username.Trim();
        if (!string.IsNullOrWhiteSpace(Email)) return Email.Trim();
        return null;
    }
}
