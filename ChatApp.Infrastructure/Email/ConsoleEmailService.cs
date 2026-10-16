using System.Net;
using System.Text.RegularExpressions;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Email;

// Development only: prints the email's link in the console instead of sending it
public class ConsoleEmailService : IEmailService
{
    private static readonly Regex LinkPattern = new("href=\"([^\"]+)\"", RegexOptions.Compiled);
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var match = LinkPattern.Match(htmlBody);
        var link = match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : "(no link found)";

        _logger.LogWarning("DEV EMAIL (not sent) | To: {To} | Subject: {Subject} | Link: {Link}",
            toEmail, subject, link);

        return Task.CompletedTask;
    }
}