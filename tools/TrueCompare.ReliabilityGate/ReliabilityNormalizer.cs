using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrueCompare.ReliabilityGate;

public static class ReliabilityNormalizer
{
    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }

    public static IReadOnlyList<string> Tokenize(string? value)
    {
        var normalized = NormalizeName(value);
        if (normalized.Length == 0)
        {
            return Array.Empty<string>();
        }

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "de", "da", "do", "das", "dos", "em", "para", "com", "sem", "the", "and", "for",
            "produto", "produtos", "frigorifico", "frigorificos", "maquina", "maquinas", "comprar"
        };

        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 1 && !stopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static int SimilarityScore(string? left, string? right)
    {
        var leftTokens = Tokenize(left).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = Tokenize(right).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0;
        }

        var intersection = leftTokens.Count(rightTokens.Contains);
        var union = leftTokens.Union(rightTokens, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = (double)intersection / union;

        var leftName = NormalizeName(left);
        var rightName = NormalizeName(right);
        if (leftName.Contains(rightName, StringComparison.OrdinalIgnoreCase)
            || rightName.Contains(leftName, StringComparison.OrdinalIgnoreCase))
        {
            jaccard = Math.Max(jaccard, 0.88);
        }

        return (int)Math.Round(jaccard * 100, MidpointRounding.AwayFromZero);
    }

    public static long ParsePriceCents(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var cleaned = value
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace("€", string.Empty, StringComparison.Ordinal)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var match = Regex.Match(cleaned, @"(?<amount>\d{1,3}(?:[.\s]\d{3})*(?:,\d{1,2})?|\d+(?:[.,]\d{1,2})?)");
        if (!match.Success)
        {
            return 0;
        }

        var amount = match.Groups["amount"].Value.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (amount.Contains(',', StringComparison.Ordinal))
        {
            amount = amount.Replace(".", string.Empty, StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal);
        }
        else if (Regex.IsMatch(amount, @"^\d{1,3}(?:\.\d{3})+$"))
        {
            amount = amount.Replace(".", string.Empty, StringComparison.Ordinal);
        }

        if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return 0;
        }

        return (long)Math.Round(parsed * 100m, MidpointRounding.AwayFromZero);
    }

    public static string FormatCurrency(long cents)
    {
        return (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-PT"));
    }

    public static bool PricesCompatible(long appPriceCents, long marketPriceCents, decimal tolerance = 0.20m)
    {
        if (appPriceCents <= 0 || marketPriceCents <= 0)
        {
            return false;
        }

        var delta = Math.Abs(appPriceCents - marketPriceCents);
        return delta <= marketPriceCents * tolerance;
    }
}
