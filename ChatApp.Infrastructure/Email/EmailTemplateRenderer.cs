using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using ChatApp.Domain.Interfaces;

namespace ChatApp.Infrastructure.Email;

public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public string Render(string templateName, IReadOnlyDictionary<string, string> values)
    {
        var template = _cache.GetOrAdd(templateName, LoadTemplate);

        // One pass over the template; every value is HTML-encoded before it goes in
        return PlaceholderPattern.Replace(template, match =>
            values.TryGetValue(match.Groups[1].Value, out var value)
                ? WebUtility.HtmlEncode(value)
                : match.Value);
    }

    private static string LoadTemplate(string templateName)
    {
        var assembly = typeof(EmailTemplateRenderer).Assembly;

        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith($".{templateName}.html", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException(
                $"Email template '{templateName}' was not found. Check that its Build Action is 'Embedded resource'.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}