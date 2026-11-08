using System;
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
    sealed class JsonParseFrame(JsonValue node)
    {
        internal readonly JsonValue Node = node;
        internal int State;
        internal string Key;
    }
    sealed class JsonParser(string text, bool remember)
    {
        int position;
        readonly Stack<JsonParseFrame> frames = new();
        Exception Invalid() => MakeThrown(new JsCaughtError("SyntaxError", "Invalid JSON at position " + position.ToString(CultureInfo.InvariantCulture)));
        void Space() { while (position < text.Length && text[position] is ' ' or '\t' or '\r' or '\n') position++; }
        bool Consume(char ch) { if (position < text.Length && text[position] == ch) { position++; return true; } return false; }
        void Expect(char ch) { if (!Consume(ch)) throw Invalid(); }
        string ReadString()
        {
            Expect('"'); int start = position; StringBuilder result = null;
            while (position < text.Length)
            {
                char ch = text[position++];
                if (ch == '"') return result is null ? text.Substring(start, position - start - 1) : result.Append(text, start, position - start - 1).ToString();
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
        JsonValue ReadValue()
        {
            Space(); int start = position;
            if (position == text.Length) throw Invalid();
            char ch = text[position]; object value;
            if (ch is '{' or '[')
            {
                position++; var container = new JsonValue(ch == '{' ? new JsDynamicObject() : new JsArray(0), start);
                if (remember) container.Children = new(StringComparer.Ordinal);
                frames.Push(new(container)); return container;
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
                Consume('-');
                if (!Consume('0')) { if (position == text.Length || text[position] is < '1' or > '9') throw Invalid(); while (position < text.Length && char.IsAsciiDigit(text[position])) position++; }
                if (Consume('.')) { int digits = position; while (position < text.Length && char.IsAsciiDigit(text[position])) position++; if (digits == position) throw Invalid(); }
                if (position < text.Length && text[position] is 'e' or 'E')
                {
                    position++; if (position < text.Length && text[position] is '+' or '-') position++;
                    int digits = position; while (position < text.Length && char.IsAsciiDigit(text[position])) position++; if (digits == position) throw Invalid();
                }
                if (!double.TryParse(text.AsSpan(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) throw Invalid();
                value = number;
            }
            return new JsonValue(value, start) { End = position };
        }
        internal JsonValue Parse()
        {
            JsonValue root = ReadValue();
            while (frames.Count != 0)
            {
                var frame = frames.Peek(); Space();
                if (frame.Node.Value is JsArray array)
                {
                    if (frame.State == 0 && Consume(']') || frame.State == 1 && Consume(']')) { frame.Node.End = position; frames.Pop(); continue; }
                    if (frame.State == 1) { Expect(','); frame.State = 2; continue; }
                    string key = array.Length().ToString(CultureInfo.InvariantCulture);
                    JsonValue child = ReadValue(); array.Append(child.Value); if (remember) frame.Node.Children[key] = child;
                    frame.State = 1;
                }
                else
                {
                    if (frame.State == 0 && Consume('}') || frame.State == 2 && Consume('}')) { frame.Node.End = position; frames.Pop(); continue; }
                    if (frame.State == 2) { Expect(','); frame.State = 3; continue; }
                    if (frame.State is 0 or 3) { frame.Key = ReadString(); frame.State = 1; continue; }
                    Expect(':'); JsonValue child = ReadValue(); ((TsObject)frame.Node.Value).DefineOwn(frame.Key, child.Value);
                    if (remember) frame.Node.Children[frame.Key] = child; frame.State = 2;
                }
            }
            Space(); if (position != text.Length) throw Invalid(); return root;
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
