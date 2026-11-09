using System;

public static partial class Js
{
    static bool IsoDigits(ReadOnlySpan<char> text, ref int position, int count, out int value)
    {
        value = 0;
        if (count > text.Length - position) return false;
        for (int index = 0; index < count; index++)
        {
            char digit = text[position++];
            if (digit is < '0' or > '9') return false;
            value = value * 10 + digit - '0';
        }
        return true;
    }
    static bool TryParseIsoDate(string input, out double timestamp)
    {
        timestamp = double.NaN;
        ReadOnlySpan<char> text = input;
        int position = 0;
        bool negative = text.Length > 0 && text[0] == '-';
        bool signed = text.Length > 0 && text[0] is '+' or '-';
        if (signed) position++;
        if (!IsoDigits(text, ref position, signed ? 6 : 4, out int year)) return false;
        if (negative) year = -year;
        int month = 1, day = 1, hour = 0, minute = 0, second = 0, milliseconds = 0;
        bool hasDay = false, hasTime = false, fractionNonzero = false;
        if (position < text.Length && text[position] == '-')
        {
            position++;
            if (!IsoDigits(text, ref position, 2, out month)) return false;
            if (position < text.Length && text[position] == '-')
            {
                position++;
                if (!IsoDigits(text, ref position, 2, out day)) return false;
                hasDay = true;
            }
        }
        if (position < text.Length && text[position] == 'T' && hasDay)
        {
            hasTime = true; position++;
            if (!IsoDigits(text, ref position, 2, out hour) || position >= text.Length || text[position++] != ':' ||
                !IsoDigits(text, ref position, 2, out minute)) return false;
            if (position < text.Length && text[position] == ':')
            {
                position++;
                if (!IsoDigits(text, ref position, 2, out second)) return false;
                if (position < text.Length && text[position] == '.')
                {
                    int start = ++position;
                    while (position < text.Length && text[position] is >= '0' and <= '9')
                    {
                        int digit = text[position] - '0';
                        if (position - start < 3) milliseconds = milliseconds * 10 + digit;
                        fractionNonzero |= digit != 0;
                        position++;
                    }
                    int digits = position - start;
                    if (digits == 0) return false;
                    for (; digits < 3; digits++) milliseconds *= 10;
                }
            }
        }
        bool zoned = false;
        double offset = 0;
        if (hasTime && position < text.Length)
        {
            char sign = text[position++];
            zoned = true;
            if (sign != 'Z')
            {
                if (sign is not ('+' or '-') || !IsoDigits(text, ref position, 2, out int hours)) return false;
                if (position < text.Length && text[position] == ':') position++;
                if (!IsoDigits(text, ref position, 2, out int minutes)) return false;
                if (hours > 23 || minutes > 59) return true;
                offset = (hours * 60 + minutes) * 60000d * (sign == '-' ? -1 : 1);
            }
        }
        if (position != text.Length) return false;
        if (negative && year == 0 || month is < 1 or > 12 || day is < 1 or > 31 || hour > 24 || minute > 59 || second > 59 ||
            hour == 24 && (minute != 0 || second != 0 || fractionNonzero)) return true;
        ReadOnlySpan<double> parts = stackalloc double[] { year, month - 1, day, hour, minute, second, milliseconds };
        if (hasTime && !zoned)
        {
            // Local calendar construction continues to use the platform timezone.
            timestamp = DateFromComponents(parts.ToArray(), false, false);
        }
        else timestamp = ClipDate(UtcDateTimestamp(year, month, parts) - offset);
        return true;
    }
}
