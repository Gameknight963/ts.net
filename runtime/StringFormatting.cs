using System;

public static partial class Js
{
    enum StringFormattingKind { Trim, TrimStart, TrimEnd, Repeat, PadStart, PadEnd }
    static bool TrimWhitespace(char value) => value is '\u0009' or '\u000a' or '\u000b' or '\u000c' or '\u000d' or '\u0020' or '\u00a0' or '\u1680' or >= '\u2000' and <= '\u200a' or '\u2028' or '\u2029' or '\u202f' or '\u205f' or '\u3000' or '\ufeff';
    static void FillRepeated(Span<char> destination, ReadOnlySpan<char> pattern)
    {
        int filled = Math.Min(destination.Length, pattern.Length);
        pattern[..filled].CopyTo(destination);
        while (filled < destination.Length)
        {
            int count = Math.Min(filled, destination.Length - filled);
            destination[..count].CopyTo(destination[filled..]);
            filled += count;
        }
    }
    sealed class StringFormattingFunction(string name, StringFormattingKind kind) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, name, kind <= StringFormattingKind.TrimEnd ? 0 : 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            if (kind <= StringFormattingKind.TrimEnd)
            {
                int first = 0, end = text.Length;
                if (kind != StringFormattingKind.TrimEnd) while (first < end && TrimWhitespace(text[first])) first++;
                if (kind != StringFormattingKind.TrimStart) while (end > first && TrimWhitespace(text[end - 1])) end--;
                return text.Substring(first, end - first);
            }
            double count = Number(FunctionArgument(arguments, 0));
            count = double.IsNaN(count) ? 0 : Math.Truncate(count);
            if (kind == StringFormattingKind.Repeat)
            {
                if (count < 0 || double.IsPositiveInfinity(count)) throw new ArgumentOutOfRangeException(nameof(arguments), "Invalid repeat count");
                if (count == 0 || text.Length == 0) return "";
                double size = text.Length * count;
                if (size > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(arguments), "String is too long");
                return string.Create((int)size, text, static (buffer, pattern) => FillRepeated(buffer, pattern));
            }
            if (count <= text.Length) return text;
            object filler = FunctionArgument(arguments, 1);
            string fill = IsUndefined(filler) ? " " : StringValue(filler);
            if (fill.Length == 0) return text;
            if (count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(arguments), "String is too long");
            return string.Create((int)count, (text, fill, start: kind == StringFormattingKind.PadStart), static (buffer, state) =>
            {
                int padding = buffer.Length - state.text.Length;
                state.text.AsSpan().CopyTo(buffer[(state.start ? padding : 0)..]);
                FillRepeated(state.start ? buffer[..padding] : buffer[state.text.Length..], state.fill);
            });
        }
    }
    static void DefineStringFormatting(JsBoxedPrimitive prototype)
    {
        var trimStart = new StringFormattingFunction("trimStart", StringFormattingKind.TrimStart);
        var trimEnd = new StringFormattingFunction("trimEnd", StringFormattingKind.TrimEnd);
        prototype.DefineHidden("trim", new StringFormattingFunction("trim", StringFormattingKind.Trim));
        prototype.DefineHidden("trimStart", trimStart); prototype.DefineHidden("trimLeft", trimStart);
        prototype.DefineHidden("trimEnd", trimEnd); prototype.DefineHidden("trimRight", trimEnd);
        prototype.DefineHidden("repeat", new StringFormattingFunction("repeat", StringFormattingKind.Repeat));
        prototype.DefineHidden("padStart", new StringFormattingFunction("padStart", StringFormattingKind.PadStart));
        prototype.DefineHidden("padEnd", new StringFormattingFunction("padEnd", StringFormattingKind.PadEnd));
    }
}
