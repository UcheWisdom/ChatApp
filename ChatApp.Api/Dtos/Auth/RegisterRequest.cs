using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Dtos.Auth;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}