using System;

public static partial class Js
{
    static void ValidateNumberingSystem(string system)
    {
        foreach (string part in system.Split('-'))
        {
            if (part.Length is < 3 or > 8) throw new ArgumentOutOfRangeException(nameof(system), "Invalid numbering system");
            foreach (char ch in part)
                if (!char.IsAsciiLetterOrDigit(ch)) throw new ArgumentOutOfRangeException(nameof(system), "Invalid numbering system");
        }
    }
    static string ResolveNumberingSystem(string requested)
    {
        // Arabic digits also require separators and sign patterns. Keep their
        // existing unsupported path until that formatting is implemented.
        if (requested is "latn" or "arab" or "arabext") return requested;
        try {NumberDigitCharacters(requested); return requested;}
        catch (NotSupportedException) {return "latn";}
    }
    static string NumberDigitCharacters(string system) => system switch
    {
        "latn" => null,
        "beng" => "\u09e6\u09e7\u09e8\u09e9\u09ea\u09eb\u09ec\u09ed\u09ee\u09ef",
        "deva" => "\u0966\u0967\u0968\u0969\u096a\u096b\u096c\u096d\u096e\u096f",
        "thai" => "\u0e50\u0e51\u0e52\u0e53\u0e54\u0e55\u0e56\u0e57\u0e58\u0e59",
        "fullwide" => "\uff10\uff11\uff12\uff13\uff14\uff15\uff16\uff17\uff18\uff19",
        "hanidec" => "\u3007\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d",
        "mathbold" => "\U0001d7ce\U0001d7cf\U0001d7d0\U0001d7d1\U0001d7d2\U0001d7d3\U0001d7d4\U0001d7d5\U0001d7d6\U0001d7d7",
        "mathdbl" => "\U0001d7d8\U0001d7d9\U0001d7da\U0001d7db\U0001d7dc\U0001d7dd\U0001d7de\U0001d7df\U0001d7e0\U0001d7e1",
        "mathsans" => "\U0001d7e2\U0001d7e3\U0001d7e4\U0001d7e5\U0001d7e6\U0001d7e7\U0001d7e8\U0001d7e9\U0001d7ea\U0001d7eb",
        "mathsanb" => "\U0001d7ec\U0001d7ed\U0001d7ee\U0001d7ef\U0001d7f0\U0001d7f1\U0001d7f2\U0001d7f3\U0001d7f4\U0001d7f5",
        "mathmono" => "\U0001d7f6\U0001d7f7\U0001d7f8\U0001d7f9\U0001d7fa\U0001d7fb\U0001d7fc\U0001d7fd\U0001d7fe\U0001d7ff",
        "adlm" => "\U0001e950\U0001e951\U0001e952\U0001e953\U0001e954\U0001e955\U0001e956\U0001e957\U0001e958\U0001e959",
        "osma" => "\U000104a0\U000104a1\U000104a2\U000104a3\U000104a4\U000104a5\U000104a6\U000104a7\U000104a8\U000104a9",
        _ => AdditionalNumberDigitCharacters(system)
    };
    static string LocalizeNumberDigits(string text, string digitCharacters)
    {
        if (digitCharacters == null) return text;
        int width = digitCharacters.Length / 10, length = text.Length;
        if (width == 2)
            foreach (char ch in text) if (char.IsAsciiDigit(ch)) length++;
        return string.Create(length, (Text: text, Digits: digitCharacters, Width: width), static (output, state) =>
        {
            int position = 0;
            foreach (char ch in state.Text)
            {
                if (!char.IsAsciiDigit(ch)) { output[position++] = ch; continue; }
                int digit = (ch - '0') * state.Width;
                output[position++] = state.Digits[digit];
                if (state.Width == 2) output[position++] = state.Digits[digit + 1];
            }
        });
    }
}
