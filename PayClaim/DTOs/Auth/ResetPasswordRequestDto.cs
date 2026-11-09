using System.ComponentModel.DataAnnotations;

namespace PayClaim.DTOs.Auth;

public class ResetPasswordRequestDto
{
    /// <summary>
    /// Username atau Email akun yang akan di-reset.
    /// </summary>
    [Required(ErrorMessage = "Username or email is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm new password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password does not match new password.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
