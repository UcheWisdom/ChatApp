using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Resend;

namespace ChatApp.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly EmailSettings _settings;

    public ResendEmailService(IResend resend, IOptions<EmailSettings> options)
    {
        _resend = resend;
        _settings = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new EmailMessage
        {
            From = _settings.From,
            Subject = subject,
            HtmlBody = htmlBody
        };
        message.To.Add(toEmail);

        await _resend.EmailSendAsync(message);
    }
}