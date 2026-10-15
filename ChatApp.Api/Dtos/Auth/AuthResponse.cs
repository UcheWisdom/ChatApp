namespace ChatApp.Api.Dtos.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    //public string DisplayName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string OtherName { get; set; } = string.Empty;
}