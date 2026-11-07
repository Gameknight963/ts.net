using System;
using System.Text;

public static partial class Js
{
    static int NormalizationBoundaryLength(string text, int index)
    {
        char value = text[index];
        // NUL and Unicode noncharacters are inert. Some host normalization
        // APIs reject them, so preserve them independently of the host tables.
        if (value == 0 || value is >= '\uFDD0' and <= '\uFDEF' or '\uFFFE' or '\uFFFF') return 1;
        if (!char.IsSurrogate(value)) return 0;
        if (!char.IsHighSurrogate(value) || index + 1 == text.Length || !char.IsLowSurrogate(text[index + 1])) return 1;
        int point = 0x10000 + ((value - 0xd800) << 10) + text[index + 1] - 0xdc00;
        return (point & 0xffff) >= 0xfffe ? 2 : 0;
    }
    static string NormalizeString(string text, NormalizationForm form)
    {
        int first = -1;
        for (int index = 0; index < text.Length; index++)
        {
            if (NormalizationBoundaryLength(text, index) != 0) { first = index; break; }
            if (char.IsHighSurrogate(text[index])) index++;
        }
        if (first < 0) return text.Normalize(form);
        var result = new StringBuilder(text.Length);
        int start = 0;
        for (int index = first; index < text.Length; index++)
        {
            int boundary = NormalizationBoundaryLength(text, index);
            if (boundary == 0) { if (char.IsHighSurrogate(text[index])) index++; continue; }
            // These code points have combining class zero and never decompose
            // or compose. Normalization cannot cross their boundaries.
            result.Append(text.Substring(start, index - start).Normalize(form));
            result.Append(text.AsSpan(index, boundary));
            index += boundary - 1; start = index + 1;
        }
        result.Append(text.Substring(start).Normalize(form)); return result.ToString();
    }
    sealed class StringNormalizationFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "normalize", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object argument = FunctionArgument(arguments, 0);
            string name = IsUndefined(argument) ? "NFC" : StringValue(argument);
            NormalizationForm form = name switch
            {
                "NFC" => NormalizationForm.FormC,
                "NFD" => NormalizationForm.FormD,
                "NFKC" => NormalizationForm.FormKC,
                "NFKD" => NormalizationForm.FormKD,
                _ => throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid normalization form")
            };
            return NormalizeString(text, form);
        }
    }
}
