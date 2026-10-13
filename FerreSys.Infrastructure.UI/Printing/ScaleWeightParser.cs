using System.Globalization;

namespace FerreSys.Infrastructure.UI.Printing;

public readonly record struct ScaleReading(decimal Kilograms, bool Stable);

public static class ScaleWeightParser
{
    public static bool TryParse(string? line, out ScaleReading reading)
    {
        reading = default;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        string text = line.Trim();
        bool stable = !StartsWithToken(text, "US");
        decimal? last = null;
        decimal? lastWithUnit = null;

        for (int index = 0; index < text.Length; index++)
        {
            if (!TryReadNumber(text, index, out decimal value, out int numberEnd))
                continue;

            index = numberEnd;
            SkipSpaces(text, ref index);
            bool hasUnit = false;
            bool grams = false;
            if (StartsWithTokenAt(text, index, "kg"))
            {
                hasUnit = true;
                index += 2;
            }
            else if (StartsWithTokenAt(text, index, "g"))
            {
                hasUnit = true;
                grams = true;
                index += 1;
            }

            decimal kilograms = grams ? value / 1000m : value;
            last = kilograms;
            if (hasUnit)
                lastWithUnit = kilograms;

            index--;
        }

        if ((lastWithUnit ?? last) is not decimal kilogramsValue)
            return false;

        reading = new ScaleReading(kilogramsValue, stable);
        return true;
    }

    private static bool TryReadNumber(string text, int index, out decimal value, out int end)
    {
        value = 0;
        end = index;
        int start = index;
        if (start >= text.Length)
            return false;

        int cursor = start;
        if (text[cursor] is '+' or '-')
        {
            if (cursor + 1 >= text.Length || !char.IsDigit(text[cursor + 1]))
                return false;
            cursor++;
        }
        else if (!char.IsDigit(text[cursor]))
        {
            return false;
        }

        int digits = cursor;
        while (digits < text.Length && char.IsDigit(text[digits]))
            digits++;
        if (digits == cursor)
            return false;

        if (digits < text.Length && text[digits] is '.' or ',' && digits + 1 < text.Length && char.IsDigit(text[digits + 1]))
        {
            digits++;
            while (digits < text.Length && char.IsDigit(text[digits]))
                digits++;
        }

        string token = text[start..digits].Replace(',', '.');
        if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return false;

        end = digits;
        return true;
    }

    private static void SkipSpaces(string text, ref int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;
    }

    private static bool StartsWithToken(string text, string token) =>
        StartsWithTokenAt(text, 0, token);

    private static bool StartsWithTokenAt(string text, int index, string token)
    {
        if (index < 0 || index + token.Length > text.Length)
            return false;

        if (!text.AsSpan(index, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase))
            return false;

        int after = index + token.Length;
        return after >= text.Length || !char.IsLetterOrDigit(text[after]);
    }
}
