namespace ChatApp.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string From { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}