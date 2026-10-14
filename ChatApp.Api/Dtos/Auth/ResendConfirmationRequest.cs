using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Dtos.Auth;

public class ResendConfirmationRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}