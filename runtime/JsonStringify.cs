using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public static partial class Js
{
    static void QuoteJson(StringBuilder output, string text)
    {
        const string hex = "0123456789abcdef";
        output.Append('"');
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { i++; continue; }
            string escape = ch switch { '"' => "\\\"", '\\' => "\\\\", '\b' => "\\b", '\f' => "\\f", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", _ => null };
            if (escape == null && ch >= 32 && !char.IsSurrogate(ch)) continue;
            output.Append(text, start, i - start);
            if (escape != null) output.Append(escape);
            else output.Append("\\u").Append(hex[ch >> 12]).Append(hex[ch >> 8 & 15]).Append(hex[ch >> 4 & 15]).Append(hex[ch & 15]);
            start = i + 1;
        }
        output.Append(text, start, text.Length - start);
        output.Append('"');
    }
    struct JsonWriteFrame
    {
        internal readonly TsObject Value;
        internal readonly bool Array, RepeatedNames, DirectArray;
        internal readonly JsArray Keys;
        internal readonly object[] Names;
        internal readonly long Length;
        internal long Position;
        internal bool Written;
        internal JsonWriteFrame(TsObject value, JsArray propertyList, object[] previousNames)
        {
            Value = value; Array = value is JsArray;
            if (Array)
            {
                DirectArray = value.GetType() == typeof(JsArray) || value is NumberArray;
                Length = DirectArray ? (long)((JsArray)value).Length() : IndexedLength(value);
            }
            else
            {
                Names = propertyList == null ? value.JsonPropertyNames(previousNames) : null;
                RepeatedNames = Names != null && ReferenceEquals(Names, previousNames);
                if (Names != null) Length = Names.LongLength;
                else { Keys = propertyList ?? StringPropertyKeys(value.OwnKeys()); Length = (long)Keys.Length(); }
            }
        }
    }
    sealed class JsonWriter
    {
        readonly JsFunction replacer;
        readonly JsArray propertyList;
        readonly string gap;
        readonly StringBuilder output = new();
        readonly List<JsonWriteFrame> frames = new();
        HashSet<TsObject> active;
        object[] previousNames;
        KeyValuePair<string, string>[] quotedNames;
        int quotedNameCount;
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
            return Prepare(holder, key, holder.Get(key));
        }
        object Prepare(TsObject holder, string key, object value)
        {
            if (value is TsObject or BigInteger && Get(value, "toJSON") is JsFunction method) value = method.InvokeWithReceiver(value, new object[] { key });
            if (replacer != null) value = replacer.InvokeWithReceiver(holder, new[] { key, value });
            return Unbox(value);
        }
        static object PrepareArrayElement(JsArray holder, long position)
        {
            object value = holder.ElementAt((uint)position);
            if (value is TsObject or BigInteger && Get(value, "toJSON") is JsFunction method)
                value = method.InvokeWithReceiver(value, new object[] { position.ToString(CultureInfo.InvariantCulture) });
            return Unbox(value);
        }
        static object Unbox(object value)
        {
            if (value is JsBoxedPrimitive boxed && boxed.Value is not JsSymbol) value = boxed.Value is double ? Number(value) : boxed.Value is string ? StringValue(value) : boxed.Value;
            return value;
        }
        static bool Omitted(object value) => IsUndefined(value) || value is JsFunction or JsSymbol;
        void WritePropertyName(string key)
        {
            // Repeated records share short names. Bound both cache size and text length.
            bool cacheable = key.Length <= 128;
            if (cacheable && quotedNames != null)
                for (int i = 0; i < quotedNameCount; i++)
                    if (ReferenceEquals(quotedNames[i].Key, key)) { output.Append(quotedNames[i].Value); return; }
            int start = output.Length;
            QuoteJson(output, key); output.Append(gap.Length != 0 ? ": " : ":");
            if (cacheable && quotedNameCount < 4)
            {
                quotedNames ??= new KeyValuePair<string, string>[4];
                quotedNames[quotedNameCount++] = new(key, output.ToString(start, output.Length - start));
            }
        }
        void Indent(int depth) { for (int i = 0; i < depth; i++) output.Append(gap); }
        bool WritePackedNumbers(NumberArray array)
        {
            if (replacer != null || array.HasIndexedAccessors)
                return false;
            double count = array.Length();
            double[] numbers = array.TryPacked(count);
            if (numbers == null) return false;
            int length = (int)count;

            output.Append('[');
            for (int i = 0; i < length; i++)
            {
                if (i != 0) output.Append(',');
                if (gap.Length != 0) { output.Append('\n'); Indent(frames.Count + 1); }
                double number = numbers[i];
                output.Append(double.IsFinite(number) ? NumberKeyString(number) : "null");
            }
            if (length != 0 && gap.Length != 0) { output.Append('\n'); Indent(frames.Count); }
            output.Append(']');
            return true;
        }
        bool Enter(TsObject value)
        {
            if (active != null) return active.Add(value);
            // Shallow trees need only a few identity comparisons. Deep trees
            // switch once to a set so cycle checks do not become quadratic.
            if (frames.Count < 16)
            {
                foreach (ref readonly var frame in CollectionsMarshal.AsSpan(frames))
                    if (ReferenceEquals(frame.Value, value)) return false;
                return true;
            }
            active = new(frames.Count + 1, ReferenceEqualityComparer.Instance);
            foreach (ref readonly var frame in CollectionsMarshal.AsSpan(frames)) active.Add(frame.Value);
            return active.Add(value);
        }
        void WriteValue(object value)
        {
            if (value is JsRawJson raw) output.Append(raw.Text);
            else if (value == null) output.Append("null");
            else if (value is string text) QuoteJson(output, text);
            else if (value is bool boolean) output.Append(boolean ? "true" : "false");
            else if (value is double number) output.Append(double.IsFinite(number) ? NumberKeyString(number) : "null");
            else if (value is TsObject obj)
            {
                if (!Enter(obj)) throw new InvalidOperationException("Cannot serialize a cyclic object");
                if (obj is NumberArray numbers && WritePackedNumbers(numbers)) { active?.Remove(obj); return; }
                var frame = new JsonWriteFrame(obj, propertyList, previousNames);
                if (frame.Names != null) previousNames = frame.Names;
                frames.Add(frame); output.Append(frame.Array ? '[' : '{');
            }
            else throw new InvalidOperationException("Unsupported JSON value");
        }
        internal object Write(object input)
        {
            if (replacer == null)
            {
                if (input == null) return "null";
                if (input is bool boolean) return boolean ? "true" : "false";
                if (input is double number) return double.IsFinite(number) ? NumberKeyString(number) : "null";
                if (input is string text)
                {
                    QuoteJson(output, text);
                    return output.ToString();
                }
            }
            return WriteStructured(input);
        }
        object WriteStructured(object input)
        {
            var holder = new JsDynamicObject(); holder.DefineOwn("", input);
            object root = Prepare(holder, ""); if (Omitted(root)) return Undefined;
            WriteValue(root);
            while (frames.Count != 0)
            {
                // WriteValue can grow the list, so do not use this reference
                // after that call. Completed frames are copied before removal.
                ref var frame = ref CollectionsMarshal.AsSpan(frames)[^1];
                if (frame.Position == frame.Length)
                {
                    var completed = frame;
                    frames.RemoveAt(frames.Count - 1); active?.Remove(completed.Value);
                    if (completed.Written && gap.Length != 0) { output.Append('\n'); Indent(frames.Count); }
                    output.Append(completed.Array ? ']' : '}'); continue;
                }
                long position = frame.Position++;
                string key = null;
                object value;
                if (frame.DirectArray && replacer == null) value = PrepareArrayElement((JsArray)frame.Value, position);
                else
                {
                    key = frame.Array ? position.ToString(CultureInfo.InvariantCulture) : frame.Names != null ? (string)frame.Names[(int)position] : (string)frame.Keys.ElementAt((uint)position);
                    value = frame.DirectArray
                        ? Prepare(frame.Value, key, ((JsArray)frame.Value).ElementAt((uint)position))
                        : Prepare(frame.Value, key);
                }
                if (Omitted(value)) { if (!frame.Array) continue; value = null; }
                if (frame.Written) output.Append(','); frame.Written = true;
                if (gap.Length != 0) { output.Append('\n'); Indent(frames.Count); }
                if (!frame.Array)
                {
                    if (frame.RepeatedNames) WritePropertyName(key);
                    else { QuoteJson(output, key); output.Append(gap.Length != 0 ? ": " : ":"); }
                }
                WriteValue(value);
            }
            return output.ToString();
        }
    }
}
