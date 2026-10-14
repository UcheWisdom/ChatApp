namespace ChatApp.Domain.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
}