using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static int UnicodeCategoryMask(params UnicodeCategory[] categories)
    {
        int mask = 0; foreach (var category in categories) mask |= 1 << (int)category;
        return mask;
    }
    static int UnicodeRegExpCategoryMask(string property)
    {
        int category = (int)(property switch {
            "Lu" or "Uppercase_Letter" => UnicodeCategory.UppercaseLetter,
            "Ll" or "Lowercase_Letter" => UnicodeCategory.LowercaseLetter,
            "Lt" or "Titlecase_Letter" => UnicodeCategory.TitlecaseLetter,
            "Lm" or "Modifier_Letter" => UnicodeCategory.ModifierLetter,
            "Lo" or "Other_Letter" => UnicodeCategory.OtherLetter,
            "Mn" or "Nonspacing_Mark" => UnicodeCategory.NonSpacingMark,
            "Mc" or "Spacing_Mark" => UnicodeCategory.SpacingCombiningMark,
            "Me" or "Enclosing_Mark" => UnicodeCategory.EnclosingMark,
            "Nd" or "Decimal_Number" or "digit" => UnicodeCategory.DecimalDigitNumber,
            "Nl" or "Letter_Number" => UnicodeCategory.LetterNumber,
            "No" or "Other_Number" => UnicodeCategory.OtherNumber,
            "Pc" or "Connector_Punctuation" => UnicodeCategory.ConnectorPunctuation,
            "Pd" or "Dash_Punctuation" => UnicodeCategory.DashPunctuation,
            "Ps" or "Open_Punctuation" => UnicodeCategory.OpenPunctuation,
            "Pe" or "Close_Punctuation" => UnicodeCategory.ClosePunctuation,
            "Pi" or "Initial_Punctuation" => UnicodeCategory.InitialQuotePunctuation,
            "Pf" or "Final_Punctuation" => UnicodeCategory.FinalQuotePunctuation,
            "Po" or "Other_Punctuation" => UnicodeCategory.OtherPunctuation,
            "Sm" or "Math_Symbol" => UnicodeCategory.MathSymbol,
            "Sc" or "Currency_Symbol" => UnicodeCategory.CurrencySymbol,
            "Sk" or "Modifier_Symbol" => UnicodeCategory.ModifierSymbol,
            "So" or "Other_Symbol" => UnicodeCategory.OtherSymbol,
            "Zs" or "Space_Separator" => UnicodeCategory.SpaceSeparator,
            "Zl" or "Line_Separator" => UnicodeCategory.LineSeparator,
            "Zp" or "Paragraph_Separator" => UnicodeCategory.ParagraphSeparator,
            "Cc" or "Control" or "cntrl" => UnicodeCategory.Control,
            "Cf" or "Format" => UnicodeCategory.Format,
            "Cs" or "Surrogate" => UnicodeCategory.Surrogate,
            "Co" or "Private_Use" => UnicodeCategory.PrivateUse,
            "Cn" or "Unassigned" => UnicodeCategory.OtherNotAssigned,
            _ => (UnicodeCategory)(-1)
        });
        if (category >= 0) return 1 << category;
        return property switch {
            "L" or "Letter" => UnicodeCategoryMask(UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.OtherLetter),
            "LC" or "Cased_Letter" => UnicodeCategoryMask(UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter),
            "M" or "Mark" or "Combining_Mark" => UnicodeCategoryMask(UnicodeCategory.NonSpacingMark, UnicodeCategory.SpacingCombiningMark, UnicodeCategory.EnclosingMark),
            "N" or "Number" => UnicodeCategoryMask(UnicodeCategory.DecimalDigitNumber, UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber),
            "P" or "Punctuation" or "punct" => UnicodeCategoryMask(UnicodeCategory.ConnectorPunctuation, UnicodeCategory.DashPunctuation, UnicodeCategory.OpenPunctuation, UnicodeCategory.ClosePunctuation, UnicodeCategory.InitialQuotePunctuation, UnicodeCategory.FinalQuotePunctuation, UnicodeCategory.OtherPunctuation),
            "S" or "Symbol" => UnicodeCategoryMask(UnicodeCategory.MathSymbol, UnicodeCategory.CurrencySymbol, UnicodeCategory.ModifierSymbol, UnicodeCategory.OtherSymbol),
            "Z" or "Separator" => UnicodeCategoryMask(UnicodeCategory.SpaceSeparator, UnicodeCategory.LineSeparator, UnicodeCategory.ParagraphSeparator),
            "C" or "Other" => UnicodeCategoryMask(UnicodeCategory.Control, UnicodeCategory.Format, UnicodeCategory.Surrogate, UnicodeCategory.PrivateUse, UnicodeCategory.OtherNotAssigned),
            _ => 0
        };
    }
    static class UnicodeRegExpCategories
    {
        static readonly Lazy<List<RegExpCodePointRange>[]> Categories = new(Build);
        static readonly Dictionary<int, RegExpCodePointRange[]> Masks = new();
        static List<RegExpCodePointRange>[] Build()
        {
            var result = new List<RegExpCodePointRange>[30];
            for (int index = 0; index < result.Length; index++) result[index] = new();
            int start = 0, previous = (int)UnicodeCategory.Control, update = 0;
            var updates = UnicodeRegExpCategoryUpdates.Values;
            for (int value = 1; value <= 0x10ffff; value++) {
                int category = value is >= 0xd800 and <= 0xdfff ? (int)UnicodeCategory.Surrogate : (int)Rune.GetUnicodeCategory(new Rune(value));
                while (update < updates.Length && value > updates[update].Last) update++;
                if (update < updates.Length && value >= updates[update].First) category = (int)updates[update].Category;
                if (category == previous) continue;
                result[previous].Add(new(start, value - 1)); start = value; previous = category;
            }
            result[previous].Add(new(start, 0x10ffff));
            return result;
        }
        internal static List<RegExpCodePointRange> Select(int mask)
        {
            lock (Masks) {
                if (!Masks.TryGetValue(mask, out var ranges)) {
                    var selected = new List<RegExpCodePointRange>();
                    var categories = Categories.Value;
                    for (int index = 0; index < categories.Length; index++) if ((mask & (1 << index)) != 0) selected.AddRange(categories[index]);
                    ranges = MergeRegExpRanges(selected).ToArray(); Masks.Add(mask, ranges);
                }
                return new(ranges);
            }
        }
    }
    static List<RegExpCodePointRange> UnicodeRegExpProperty(string property)
    {
        foreach (char ch in property) if (!(char.IsAsciiLetterOrDigit(ch) || ch is '_' or '=')) throw UnicodeRegExpSyntax();
        int separator = property.IndexOf('=');
        if (separator >= 0) {
            string kind = property[..separator], value = property[(separator + 1)..];
            if (value.Length == 0 || value.Contains('=')) throw UnicodeRegExpSyntax();
            if (kind is "Script" or "sc" or "Script_Extensions" or "scx") throw new NotSupportedException("Unicode regular-expression scripts are not supported yet");
            if (kind is not ("General_Category" or "gc")) throw UnicodeRegExpSyntax();
            int categoryMask = UnicodeRegExpCategoryMask(value);
            if (categoryMask == 0) throw UnicodeRegExpSyntax();
            return UnicodeRegExpCategories.Select(categoryMask);
        }
        int mask = UnicodeRegExpCategoryMask(property);
        if (mask != 0) return UnicodeRegExpCategories.Select(mask);
        switch (property) {
            case "Any": return [new(0, 0x10ffff)];
            case "ASCII": return [new(0, 127)];
            case "Assigned": return UnicodeRegExpCategories.Select(~(1 << (int)UnicodeCategory.OtherNotAssigned));
            case "ASCII_Hex_Digit": case "AHex": return [new('0', '9'), new('A', 'F'), new('a', 'f')];
            case "Hex_Digit": case "Hex": return [new('0', '9'), new('A', 'F'), new('a', 'f'), new(0xff10, 0xff19), new(0xff21, 0xff26), new(0xff41, 0xff46)];
            case "White_Space": case "space": return [new(9, 13), new(32, 32), new(0x85, 0x85), new(0xa0, 0xa0), new(0x1680, 0x1680), new(0x2000, 0x200a), new(0x2028, 0x2029), new(0x202f, 0x202f), new(0x205f, 0x205f), new(0x3000, 0x3000)];
            case "Pattern_White_Space": case "Pat_WS": return [new(9, 13), new(32, 32), new(0x85, 0x85), new(0x200e, 0x200f), new(0x2028, 0x2029)];
            case "Join_Control": case "Join_C": return [new(0x200c, 0x200d)];
            case "Bidi_Control": case "Bidi_C": return [new(0x61c, 0x61c), new(0x200e, 0x200f), new(0x202a, 0x202e), new(0x2066, 0x2069)];
            case "Regional_Indicator": case "RI": return [new(0x1f1e6, 0x1f1ff)];
            case "Emoji_Modifier": case "EMod": return [new(0x1f3fb, 0x1f3ff)];
            case "Variation_Selector": case "VS": return [new(0x180b, 0x180d), new(0x180f, 0x180f), new(0xfe00, 0xfe0f), new(0xe0100, 0xe01ef)];
            case "Noncharacter_Code_Point": case "NChar":
                var ranges = new List<RegExpCodePointRange>{new(0xfdd0, 0xfdef)};
                for (int plane = 0; plane <= 16; plane++) ranges.Add(new(plane * 0x10000 + 0xfffe, plane * 0x10000 + 0xffff));
                return ranges;
        }
        if (UnicodeRegExpBinaryProperties.TrySelect(property, out var binary)) return binary;
        throw UnicodeRegExpSyntax();
    }
}
