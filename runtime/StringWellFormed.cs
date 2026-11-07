using System;

public static partial class Js
{
    static int FirstUnpairedSurrogate(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            char value = text[index];
            if (!char.IsSurrogate(value)) continue;
            if (char.IsHighSurrogate(value) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])) { index++; continue; }
            return index;
        }
        return -1;
    }
    sealed class StringWellFormedFunction(bool repair) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, repair ? "toWellFormed" : "isWellFormed", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            int first = FirstUnpairedSurrogate(text);
            if (!repair) return BoxBoolean(first < 0);
            if (first < 0) return text;
            return string.Create(text.Length, (text, first), static (buffer, state) =>
            {
                state.text.AsSpan().CopyTo(buffer);
                for (int index = state.first; index < buffer.Length; index++)
                {
                    char value = buffer[index];
                    if (!char.IsSurrogate(value)) continue;
                    if (char.IsHighSurrogate(value) && index + 1 < buffer.Length && char.IsLowSurrogate(buffer[index + 1])) { index++; continue; }
                    buffer[index] = '\uFFFD';
                }
            });
        }
    }
}
