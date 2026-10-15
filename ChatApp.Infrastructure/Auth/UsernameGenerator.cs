using System.Globalization;
using System.Text;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Infrastructure.Auth;

public class UsernameGenerator : IUsernameGenerator
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsernameGenerator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string> GenerateAsync(string firstName, string otherName)
    {
        var baseName = $"{Clean(firstName)}.{Clean(otherName)}".Trim('.');
        if (baseName.Length == 0)
            baseName = "user";

        if (baseName.Length > 24)
            baseName = baseName[..24].TrimEnd('.');

        var candidate = baseName;

        for (var i = 0; i < 5 && await _userManager.FindByNameAsync(candidate) is not null; i++)
            candidate = $"{baseName}{Random.Shared.Next(100, 10000)}";

        return candidate;
    }

    // "Ọlá Adé" -> "olaade": lowercase, strip accents, keep only letters and digits
    private static string Clean(string input)
    {
        var sb = new StringBuilder();

        foreach (var c in input.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsAsciiLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}