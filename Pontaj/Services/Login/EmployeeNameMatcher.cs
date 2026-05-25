using System.Globalization;
using System.Text;
using Pontaj.Repositories;

namespace Pontaj.Services.Login;

public static class EmployeeNameMatcher
{
    private static readonly char[] UsernameSeparators = { '.', '_' };
    private static readonly char[] NameWordSeparators = { ' ', '-' };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static int? Match(string adUsername, IReadOnlyCollection<EmployeeNameRow> candidates)
    {
        if (string.IsNullOrWhiteSpace(adUsername) || candidates.Count == 0)
        {
            return null;
        }

        var parts = adUsername.Split(UsernameSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var firstToken = Normalize(parts[0]);
        var lastToken = Normalize(parts[^1]);
        if (firstToken.Length == 0 || lastToken.Length == 0)
        {
            return null;
        }

        var matchedIds = new List<int>(2);
        foreach (var candidate in candidates)
        {
            if (Normalize(candidate.LastName) == lastToken
                && FirstWord(candidate.FirstName) == firstToken)
            {
                matchedIds.Add(candidate.Id);
                if (matchedIds.Count > 1)
                {
                    return null;
                }
            }
        }

        return matchedIds.Count == 1 ? matchedIds[0] : null;
    }

    private static string FirstWord(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return string.Empty;
        }

        var words = firstName.Split(NameWordSeparators, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? string.Empty : Normalize(words[0]);
    }
}
