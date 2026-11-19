using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static partial class Js
{
    sealed class JsonValue(object value, int start)
    {
        internal readonly object Value = value;
        internal readonly int Start = start;
        internal int End;
        internal Dictionary<string, JsonValue> Children;
    }
    sealed class JsonKeyCache
    {
        string first, second, third;
        object fourth;
        sealed class AdditionalKeys(string fourth)
        {
            Dictionary<string, string> names;
            internal string Read(ReadOnlySpan<char> text)
            {
                if (text.SequenceEqual(fourth)) return fourth;
                if (text.Length > 256) return text.ToString();
                if (names != null && names.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(text, out string cached)) return cached;
                string value = text.ToString();
                names ??= new(StringComparer.Ordinal);
                if (names.Count < 128) names.TryAdd(value, value);
                return value;
            }
        }
        internal string Read(ReadOnlySpan<char> text)
        {
            if (first != null && text.SequenceEqual(first)) return first;
            if (second != null && text.SequenceEqual(second)) return second;
            if (third != null && text.SequenceEqual(third)) return third;
            if (fourth is AdditionalKeys additional) return additional.Read(text);
            if (fourth is string key && text.SequenceEqual(key)) {
                // Repetition justifies the larger cache. A single object with
                // unique keys keeps the same small header as before.
                fourth = new AdditionalKeys(key);
                return key;
            }
            string value = text.ToString();
            if (first == null) first = value;
            else if (second == null) second = value;
            else if (third == null) third = value;
            else if (fourth == null) fourth = value;
            return value;
        }
    }
    sealed class JsonParseFrame(object value, JsonValue node, JsonKeyCache keys)
    {
        internal JsonKeyCache Keys = keys;
        internal readonly object Value = value;
        internal readonly JsonValue Node = node;
        internal int State;
        internal string Key;
    }
    sealed class JsonParser(string text, bool remember)
    {
        static readonly SearchValues<char> StringStops = SearchValues.Create(
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000a\u000b\u000c\u000d\u000e\u000f" +
            "\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f\"\\");
        int position;
        JsonRecordShape recordShape;
        bool recordShapeAttempted;
        readonly Stack<JsonParseFrame> frames = new();
        Exception Invalid() => MakeThrown(new JsCaughtError("SyntaxError", "Invalid JSON at position " + position.ToString(CultureInfo.InvariantCulture)));
        void Space() { while (position < text.Length && text[position] is ' ' or '\t' or '\r' or '\n') position++; }
        bool Consume(char ch) { if (position < text.Length && text[position] == ch) { position++; return true; } return false; }
        void Expect(char ch) { if (!Consume(ch)) throw Invalid(); }
        string ReadString(JsonKeyCache keys = null)
        {
            Expect('"'); int start = position; StringBuilder result = null;
            while (position < text.Length)
            {
                int next = text.AsSpan(position).IndexOfAny(StringStops);
                if (next < 0) { position = text.Length; throw Invalid(); }
                position += next;
                char ch = text[position++];
                if (ch == '"')
                {
                    if (result != null) return result.Append(text, start, position - start - 1).ToString();
                    var value = text.AsSpan(start, position - start - 1);
                    return keys == null ? value.ToString() : keys.Read(value);
                }
                if (ch < 32) throw Invalid();
                if (ch != '\\') continue;
                result ??= new StringBuilder(); result.Append(text, start, position - start - 1);
                if (position == text.Length) throw Invalid();
                char escape = text[position++];
                if (escape == 'u')
                {
                    int code = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (position == text.Length) throw Invalid();
                        char digit = text[position++];
                        int value = digit is >= '0' and <= '9' ? digit - '0' : digit is >= 'a' and <= 'f' ? digit - 'a' + 10 : digit is >= 'A' and <= 'F' ? digit - 'A' + 10 : -1;
                        if (value < 0) throw Invalid(); code = code * 16 + value;
                    }
                    result.Append((char)code);
                }
                else result.Append(escape switch { '"' => '"', '\\' => '\\', '/' => '/', 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', _ => throw Invalid() });
                start = position;
            }
            throw Invalid();
        }
        double ReadNumber()
        {
            int start = position;
            bool negative = Consume('-');
            int digitsStart = position;
            ulong integer = 0;
            if (!Consume('0')) {
                if (position == text.Length || text[position] is < '1' or > '9') throw Invalid();
                while (position < text.Length && char.IsAsciiDigit(text[position])) {
                    integer = unchecked(integer * 10 + (uint)(text[position] - '0'));
                    position++;
                }
            }
            int integerEnd = position;
            if (Consume('.')) { int digits = position; while (position < text.Length && char.IsAsciiDigit(text[position])) position++; if (digits == position) throw Invalid(); }
            if (position < text.Length && text[position] is 'e' or 'E')
            {
                position++; if (position < text.Length && text[position] is '+' or '-') position++;
                int digits = position; while (position < text.Length && char.IsAsciiDigit(text[position])) position++; if (digits == position) throw Invalid();
            }
            double number;
            // Every integer with at most 15 decimal digits is exactly
            // representable as a double. Longer tokens use the full parser,
            // even if the unchecked accumulator wrapped while scanning.
            if (position == integerEnd && integerEnd - digitsStart <= 15)
                number = negative ? -(double)integer : integer;
            else if (!double.TryParse(text.AsSpan(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) throw Invalid();
            return number;
        }
        bool TryReadRecordReference(out object value)
        {
            value = null;
            if (position == text.Length) return false;
            char ch = text[position];
            if (ch == '"') { value = ReadString(); return true; }
            string literal = ch == 'n' ? "null" : ch == 't' ? "true" : ch == 'f' ? "false" : null;
            if (literal == null || !text.AsSpan(position).StartsWith(literal, StringComparison.Ordinal)) return false;
            position += literal.Length;
            value = ch == 'n' ? null : BoxBoolean(ch == 't');
            return true;
        }
        bool TryReadRecord(out object value)
        {
            value = null;
            if (remember || recordShapeAttempted && recordShape == null) return false;
            int saved = position;
            position++; Space();
            if (recordShape == null)
            {
                recordShapeAttempted = true;
                var names = new List<string>();
                var numbers = new List<double>();
                object[] references = null;
                uint referenceFields = 0;
                while (names.Count < 32 && position < text.Length && text[position] == '"')
                {
                    string key = ReadString(); Space();
                    // Subsequent records compare unescaped source text. Keys
                    // requiring JSON escaping keep the ordinary parser.
                    bool simple = true;
                    foreach (char character in key) if (character < 32 || character is '"' or '\\') { simple = false; break; }
                    if (!simple) break;
                    if (!Consume(':')) break;
                    Space();
                    if (position == text.Length || names.Contains(key)) break;
                    double number = 0;
                    if (text[position] == '-' || char.IsAsciiDigit(text[position])) number = ReadNumber();
                    else {
                        if (!TryReadRecordReference(out object reference)) break;
                        (references ??= new object[32])[names.Count] = reference;
                        referenceFields |= 1u << names.Count;
                    }
                    names.Add(key); numbers.Add(number); Space();
                    if (Consume('}'))
                    {
                        recordShape = new(names.ToArray(), DefaultClassObjectPrototype, referenceFields);
                        value = references == null ? new JsonNumericRecord(recordShape, numbers.ToArray())
                            : new JsonMixedRecord(recordShape, numbers.ToArray(), references.AsSpan(0, names.Count).ToArray());
                        return true;
                    }
                    if (!Consume(',')) break;
                    Space();
                }
            }
            else
            {
                double[] numbers = new double[recordShape.Names.Length];
                object[] references = recordShape.References == 0 ? null : new object[numbers.Length];
                for (int i = 0; i < numbers.Length; i++)
                {
                    string key = recordShape.Names[i];
                    if (!Consume('"')) break;
                    if (!text.AsSpan(position).StartsWith(key, StringComparison.Ordinal)) break;
                    position += key.Length;
                    if (!Consume('"')) break;
                    Space(); if (!Consume(':')) break;
                    Space();
                    if ((recordShape.References & (1u << i)) != 0) {
                        if (!TryReadRecordReference(out references[i])) break;
                    }
                    else {
                        if (position == text.Length || !(text[position] == '-' || char.IsAsciiDigit(text[position]))) break;
                        numbers[i] = ReadNumber();
                    }
                    Space();
                    if (i == numbers.Length - 1)
                    {
                        if (!Consume('}')) break;
                        value = references == null ? new JsonNumericRecord(recordShape, numbers)
                            : new JsonMixedRecord(recordShape, numbers, references);
                        return true;
                    }
                    if (!Consume(',')) break;
                    Space();
                }
            }
            position = saved;
            return false;
        }
        object ReadValue(out JsonValue record)
        {
            record = null;
            Space(); int start = position;
            if (position == text.Length) throw Invalid();
            char ch = text[position]; object value;
            if (ch is '{' or '[')
            {
                if (ch == '{' && frames.Count != 0 && frames.Peek().Value is JsArray && TryReadRecord(out value)) return value;
                position++;
                if (ch == '[' && !remember) Space();
                bool packed = ch == '[' && !remember && position < text.Length &&
                    (text[position] == '-' || char.IsAsciiDigit(text[position]));
                value = ch == '{' ? new JsDynamicObject() : packed ? new TaggedArray(0) : new JsArray(0);
                if (remember) record = new JsonValue(value, start) { Children = new(StringComparer.Ordinal) };
                frames.Push(new(value, record, frames.Count == 0 ? null : frames.Peek().Keys)); return value;
            }
            if (ch == '"') value = ReadString();
            else if (ch is 'n' or 't' or 'f')
            {
                string literal = ch == 'n' ? "null" : ch == 't' ? "true" : "false";
                if (!text.AsSpan(position).StartsWith(literal, StringComparison.Ordinal)) throw Invalid();
                position += literal.Length; value = ch == 'n' ? null : BoxBoolean(ch == 't');
            }
            else
            {
                value = ReadNumber();
            }
            if (remember) record = new JsonValue(value, start) { End = position };
            return value;
        }
        internal JsonValue Parse()
        {
            Space(); int start = position;
            object root = ReadValue(out JsonValue rootRecord);
            while (frames.Count != 0)
            {
                var frame = frames.Peek(); Space();
                if (frame.Value is JsArray array)
                {
                    if (frame.State == 0 && Consume(']') || frame.State == 1 && Consume(']')) { if (frame.Node != null) frame.Node.End = position; frames.Pop(); continue; }
                    if (frame.State == 1) { Expect(','); frame.State = 2; continue; }
                    if (array is TaggedArray packed && position < text.Length &&
                        (text[position] == '-' || char.IsAsciiDigit(text[position]))) {
                        packed.AppendUnexposedNumber(ReadNumber());
                        frame.State = 1;
                        continue;
                    }
                    string key = remember ? array.Length().ToString(CultureInfo.InvariantCulture) : null;
                    object child = ReadValue(out JsonValue childRecord);
                    if (array is TaggedArray mixed) mixed.AppendParsedReference(child); else array.Append(child);
                    if (remember) frame.Node.Children[key] = childRecord;
                    frame.State = 1;
                }
                else
                {
                    if (frame.State == 0 && Consume('}') || frame.State == 2 && Consume('}')) { if (frame.Node != null) frame.Node.End = position; frames.Pop(); continue; }
                    if (frame.State == 2) { Expect(','); frame.State = 3; continue; }
                    if (frame.State is 0 or 3)
                    {
                        if (frame.Keys == null)
                        {
                            var keys = new JsonKeyCache();
                            foreach (var owner in frames) owner.Keys = keys;
                        }
                        frame.Key = ReadString(frame.Keys); frame.State = 1; continue;
                    }
                    Expect(':'); object child = ReadValue(out JsonValue childRecord); ((TsObject)frame.Value).DefineOwn(frame.Key, child);
                    if (remember) frame.Node.Children[frame.Key] = childRecord; frame.State = 2;
                }
            }
            int end = position;
            Space(); if (position != text.Length) throw Invalid();
            return rootRecord ?? new JsonValue(root, start) { End = end };
        }
    }
    sealed class JsonReviveFrame
    {
        internal readonly TsObject Holder;
        internal readonly string Key;
        internal readonly object Value;
        internal readonly JsonValue Record;
        internal readonly JsArray Keys;
        internal readonly long Length;
        internal long Position;
        internal JsonReviveFrame(TsObject holder, string key, JsonValue record)
        {
            Holder = holder; Key = key; Value = holder.Get(key);
            Record = record != null && SameValue(Value, record.Value) ? record : null;
            if (Value is JsArray array) Length = IndexedLength(array);
            else if (Value is TsObject obj) { Keys = (JsArray)obj.OwnKeys(); Length = (long)Keys.Length(); }
        }
    }
    static object ReviveJson(string text, JsonValue root, JsFunction reviver)
    {
        var holder = new JsDynamicObject(); holder.DefineOwn("", root.Value);
        var frames = new Stack<JsonReviveFrame>(); frames.Push(new(holder, "", root));
        while (true)
        {
            var frame = frames.Peek();
            if (frame.Position < frame.Length)
            {
                string key = frame.Keys != null ? (string)frame.Keys.ElementAt((uint)frame.Position) : frame.Position.ToString(CultureInfo.InvariantCulture);
                frame.Position++; JsonValue record = null; frame.Record?.Children?.TryGetValue(key, out record);
                frames.Push(new((TsObject)frame.Value, key, record)); continue;
            }
            var context = new JsDynamicObject();
            if (frame.Record != null && frame.Value is not TsObject) context.DefineOwn("source", text.Substring(frame.Record.Start, frame.Record.End - frame.Record.Start));
            object value = reviver.InvokeWithReceiver(frame.Holder, new[] { frame.Key, frame.Value, context });
            frames.Pop(); if (frames.Count == 0) return value;
            if (IsUndefined(value)) frame.Holder.DeleteOwn(frame.Key);
            else if (!frame.Holder.HasOwn(frame.Key) || Truthy(((TsObject)frame.Holder.OwnPropertyDescriptor(frame.Key)).Get("configurable")))
                DefineJsonValue(frame.Holder, frame.Key, value);
        }
    }
    static void DefineJsonValue(TsObject holder, string key, object value)
    {
        if (holder is JsArray array && JsArray.Index(key, out _))
            TryApplyPropertyDefinition(array, key, new(value, null, null, true, true, true,
                DescriptorFields.Value | DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable));
        else if (holder is JsDescriptorObject described && described.HasPropertyDescriptors)
            TryApplyPropertyDefinition(described, key, new(value, null, null, true, true, true,
                DescriptorFields.Value | DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable));
        else holder.DefineOwn(key, value);
    }
    static TsObject CreateJsonNamespace()
    {
        var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
        result.DefineMethod(PropertyKey(ToStringTagSymbol), "JSON", false);
        result.DefineMethod("rawJSON", new JsFunction((FunctionBody)(static arguments => CreateRawJson(FunctionArgument(arguments, 0))), null, "rawJSON", 1));
        result.DefineMethod("isRawJSON", new JsFunction((FunctionBody)(static arguments => BoxBoolean(FunctionArgument(arguments, 0) is JsRawJson)), null, "isRawJSON", 1));
        result.DefineMethod("stringify", new JsFunction((FunctionBody)(static arguments =>
            new JsonWriter(FunctionArgument(arguments, 1), FunctionArgument(arguments, 2)).Write(FunctionArgument(arguments, 0))), null, "stringify", 3));
        result.DefineMethod("parse", new JsFunction((FunctionBody)(static arguments =>
        {
            string text = StringValue(FunctionArgument(arguments, 0));
            var reviver = FunctionArgument(arguments, 1) as JsFunction;
            var root = new JsonParser(text, reviver != null).Parse();
            return reviver is null ? root.Value : ReviveJson(text, root, reviver);
        }), null, "parse", 2));
        return result;
    }
}
