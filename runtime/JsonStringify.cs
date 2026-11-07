using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    static void QuoteJson(StringBuilder output, string text)
    {
        const string hex = "0123456789abcdef";
        output.Append('"');
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            string escape = ch switch { '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\f' => "\\f", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", _ => null };
            if (escape != null) { output.Append(escape); continue; }
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { output.Append(ch).Append(text[++i]); continue; }
            if (ch < 32 || char.IsSurrogate(ch)) output.Append("\\u").Append(hex[ch >> 12]).Append(hex[ch >> 8 & 15]).Append(hex[ch >> 4 & 15]).Append(hex[ch & 15]);
            else output.Append(ch);
        }
        output.Append('"');
    }
    sealed class JsonWriteFrame
    {
        internal readonly TsObject Value;
        internal readonly bool Array;
        internal readonly JsArray Keys;
        internal readonly long Length;
        internal long Position;
        internal bool Written;
        internal JsonWriteFrame(TsObject value, JsArray propertyList)
        {
            Value = value; Array = value is JsArray;
            if (Array) Length = IndexedLength(value);
            else { Keys = propertyList ?? (JsArray)value.OwnKeys(); Length = (long)Keys.Length(); }
        }
    }
    sealed class JsonWriter
    {
        readonly JsFunction replacer;
        readonly JsArray propertyList;
        readonly string gap;
        readonly StringBuilder output = new();
        readonly Stack<JsonWriteFrame> frames = new();
        readonly HashSet<TsObject> active = new(ReferenceEqualityComparer.Instance);
        internal JsonWriter(object replacement, object space)
        {
            replacer = replacement as JsFunction;
            if (replacement is JsArray array)
            {
                propertyList = new JsArray(0); var seen = new HashSet<string>(StringComparer.Ordinal);
                long length = IndexedLength(array);
                for (long index = 0; index < length; index++)
                {
                    object value = array.Get(index.ToString(CultureInfo.InvariantCulture));
                    if (value is string or double || value is JsBoxedPrimitive boxed && boxed.Value is string or double)
                    { string key = StringValue(value); if (seen.Add(key)) propertyList.Append(key); }
                }
            }
            if (space is JsBoxedPrimitive primitive)
            {
                if (primitive.Value is double) space = Number(space);
                else if (primitive.Value is string) space = StringValue(space);
            }
            gap = space is double number ? new string(' ', double.IsNaN(number) || number <= 0 ? 0 : (int)Math.Min(10, Math.Truncate(number))) : space is string text ? text[..Math.Min(10, text.Length)] : "";
        }
        object Prepare(TsObject holder, string key)
        {
            object value = holder.Get(key);
            if (value is TsObject obj && obj.Get("toJSON") is JsFunction method) value = method.InvokeWithReceiver(obj, new object[] { key });
            if (replacer != null) value = replacer.InvokeWithReceiver(holder, new[] { key, value });
            if (value is JsBoxedPrimitive boxed) value = boxed.Value is double ? Number(value) : boxed.Value is string ? StringValue(value) : boxed.Value;
            return value;
        }
        static bool Omitted(object value) => IsUndefined(value) || value is JsFunction;
        void Indent(int depth) { for (int i = 0; i < depth; i++) output.Append(gap); }
        void WriteValue(object value)
        {
            if (value == null) output.Append("null");
            else if (value is string text) QuoteJson(output, text);
            else if (value is bool boolean) output.Append(boolean ? "true" : "false");
            else if (value is double number) output.Append(double.IsFinite(number) ? StringValue(number) : "null");
            else if (value is TsObject obj)
            {
                if (!active.Add(obj)) throw new InvalidOperationException("Cannot serialize a cyclic object");
                var frame = new JsonWriteFrame(obj, propertyList); frames.Push(frame); output.Append(frame.Array ? '[' : '{');
            }
            else throw new InvalidOperationException("Unsupported JSON value");
        }
        internal object Write(object input)
        {
            var holder = new JsDynamicObject(); holder.DefineOwn("", input);
            object root = Prepare(holder, ""); if (Omitted(root)) return Undefined;
            WriteValue(root);
            while (frames.Count != 0)
            {
                var frame = frames.Peek();
                if (frame.Position == frame.Length)
                {
                    frames.Pop(); active.Remove(frame.Value);
                    if (frame.Written && gap.Length != 0) { output.Append('\n'); Indent(frames.Count); }
                    output.Append(frame.Array ? ']' : '}'); continue;
                }
                string key = frame.Array ? frame.Position.ToString(CultureInfo.InvariantCulture) : (string)frame.Keys.ElementAt((uint)frame.Position);
                frame.Position++; object value = Prepare(frame.Value, key);
                if (Omitted(value)) { if (!frame.Array) continue; value = null; }
                if (frame.Written) output.Append(','); frame.Written = true;
                if (gap.Length != 0) { output.Append('\n'); Indent(frames.Count); }
                if (!frame.Array) { QuoteJson(output, key); output.Append(gap.Length != 0 ? ": " : ":"); }
                WriteValue(value);
            }
            return output.ToString();
        }
    }
}
