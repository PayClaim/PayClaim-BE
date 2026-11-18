using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.Auth;

public class ResetPasswordRequestDto
{
    /// <summary>
    /// Email akun yang akan di-reset.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm new password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match new password.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
