using System.Globalization;

namespace SimpleMenu.App;

public static class NumberInput
{
    public static bool TryParseDecimal(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized = text.Trim().Replace(" ", "");
        normalized = NormalizeSeparators(normalized);

        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string NormalizeSeparators(string text)
    {
        var hasDot = text.Contains('.');
        var hasComma = text.Contains(',');

        if (hasDot && hasComma)
        {
            return text.LastIndexOf('.') > text.LastIndexOf(',')
                ? text.Replace(",", "")
                : text.Replace(".", "").Replace(',', '.');
        }

        if (!hasComma)
        {
            return text;
        }

        var parts = text.Split(',');
        if (parts.Length == 2 && parts[1].Length == 3 && parts[0].Length > 0)
        {
            return text.Replace(",", "");
        }

        return text.Replace(',', '.');
    }
}
