using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

public static partial class Js
{
    [ThreadStatic] static Dictionary<int, int> dotContextClasses;
    static bool DotContextBoundary(Rune rune) => CaseCombiningClass(rune) != -1;
    // Only starter (0), above (230), and other (-1) classes affect casing contexts.
    static int CaseCombiningClass(Rune rune)
    {
        if (Rune.GetUnicodeCategory(rune) is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.SpacingCombiningMark)) return 0;
        if (rune.Value is >= 0x300 and <= 0x314 or >= 0x33d and <= 0x344) return 230;
        // Canonical ordering commutes with a class-230 mark exactly when this
        // mark has a different nonzero class. Two distinct reference marks
        // also handle a character that decomposes to one of the references.
        var cache = dotContextClasses ??= new Dictionary<int, int>();
        if (cache.TryGetValue(rune.Value, out int cached)) return cached;
        string mark = rune.ToString();
        bool Commutes(string reference) => (reference + mark).Normalize(NormalizationForm.FormD) == (mark + reference).Normalize(NormalizationForm.FormD);
        bool boundary = !Commutes("\u0300") || !Commutes("\u0301");
        int classification = !boundary ? -1 : Commutes("\u0334") ? 230 : 0;
        if (cache.Count >= 256) cache.Clear();
        cache.Add(rune.Value, classification);
        return classification;
    }
    static bool TurkicDotContext(string text, int index, bool backward)
    {
        while ((uint)index < (uint)text.Length)
        {
            if (backward && char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1])) index--;
            if (!Rune.TryGetRuneAt(text, index, out var rune)) return false;
            if (rune.Value == (backward ? 'I' : '\u0307')) return true;
            if (DotContextBoundary(rune)) return false;
            index += backward ? -1 : rune.Utf16SequenceLength;
        }
        return false;
    }
    static string ApplyTurkicDotContext(string text)
    {
        int first = text.IndexOf('I');
        if (first < 0) return text;
        StringBuilder result = null;
        int copied = 0;
        for (int index = first; index < text.Length; index++)
        {
            string replacement = text[index] switch
            {
                'I' when TurkicDotContext(text, index + 1, false) => "i",
                '\u0307' when TurkicDotContext(text, index - 1, true) => "",
                _ => null
            };
            if (replacement == null) continue;
            result ??= new StringBuilder(text.Length);
            result.Append(text, copied, index - copied).Append(replacement);
            copied = index + 1;
        }
        return result == null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }
}
