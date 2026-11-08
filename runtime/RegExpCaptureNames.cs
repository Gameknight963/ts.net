using System;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static string ReadRegExpCaptureName(string source)
    {
        var decoded = new StringBuilder(source.Length);
        for (int index = 0; index < source.Length; index++) {
            if (source[index] != '\\') { decoded.Append(source[index]); continue; }
            if (++index >= source.Length || source[index] != 'u') throw InvalidCaptureName();
            int start = ++index, count = 4;
            bool braces = index < source.Length && source[index] == '{';
            if (braces) {start = ++index; int end = source.IndexOf('}', start); if (end < 0) throw InvalidCaptureName(); count = end - start;}
            if (count == 0 || start + count > source.Length || !int.TryParse(source.AsSpan(start, count), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value) || value < 0 || value > 0x10ffff) throw InvalidCaptureName();
            decoded.Append(value <= 0xffff ? ((char)value).ToString() : char.ConvertFromUtf32(value));
            index = start + count - (braces ? 0 : 1);
        }
        string name = decoded.ToString();
        bool first = true;
        foreach (var rune in name.EnumerateRunes()) {
            var category = Rune.GetUnicodeCategory(rune);
            bool start = rune.Value is '$' or '_' or 0x1885 or 0x1886 or 0x2118 or 0x212e or 0x309b or 0x309c ||
                category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber;
            bool continuation = start || category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation ||
                rune.Value is 0x200c or 0x200d or 0x00b7 or 0x0387 or 0x19da or 0x30fb or 0xff65 || rune.Value is >= 0x1369 and <= 0x1371;
            if (first ? !start : !continuation) throw InvalidCaptureName();
            first = false;
        }
        if (first) throw InvalidCaptureName();
        return name;
    }
    static Exception InvalidCaptureName() => MakeThrown(new JsCaughtError("SyntaxError", "Invalid capture name"));
    internal static int[] RegExpCaptureIndices(System.Text.RegularExpressions.Regex engine, string[] names)
    {
        if (names.Length == 0) return Array.Empty<int>();
        var indices = new int[names.Length];
        for (int index = 0; index < names.Length; index++) indices[index] = engine.GroupNumberFromName(RegExpEngineCaptureName(names[index]));
        return indices;
    }
    static string RegExpEngineCaptureName(string name)
    {
        var encoded = new StringBuilder("tsnetNamed");
        foreach (char unit in name) encoded.Append(((int)unit).ToString("X4", CultureInfo.InvariantCulture));
        return encoded.ToString();
    }
}
