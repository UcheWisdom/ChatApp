namespace ChatApp.Domain.Interfaces;

public interface IEmailTemplateRenderer
{
    string Render(string templateName, IReadOnlyDictionary<string, string> values);
}