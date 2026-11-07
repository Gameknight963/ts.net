using System;
using System.Buffers;
using System.Text;

public static partial class Js
{
    sealed class TextEncoderValue : TsObject
    {
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.TextEncoder.Get("prototype");
    }
    sealed class TextDecoderValue(string name, Encoding encoding, bool fatal, bool ignoreBom) : TsObject
    {
        internal readonly string Name = name;
        internal readonly bool Fatal = fatal, IgnoreBom = ignoreBom;
        Decoder decoder;
        bool streaming, bomSeen;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.TextDecoder.Get("prototype");
        internal string Decode(ReadOnlySpan<byte> bytes, bool stream)
        {
            bool resume = streaming;
            if (!resume) bomSeen = false;
            streaming = stream;
            string text;
            try
            {
                if (!resume && !stream) text = encoding.GetString(bytes);
                else
                {
                    if (!resume) decoder = encoding.GetDecoder();
                    var chars = new char[checked(encoding.GetMaxCharCount(bytes.Length) + 2)];
                    int count = decoder.GetChars(bytes, chars, !stream);
                    text = new string(chars, 0, count);
                }
            }
            catch (DecoderFallbackException error) { throw new InvalidOperationException("Encoded data is invalid", error); }
            if (!bomSeen && text.Length != 0)
            {
                bomSeen = true;
                if (!IgnoreBom && Name is ("utf-8" or "utf-16le" or "utf-16be") && text[0] == '\ufeff') return text[1..];
            }
            return text;
        }
    }
    static Span<byte> TypedArrayBytes(TypedArrayValue array) => array.OutOfBounds ? Span<byte>.Empty : array.Buffer.Bytes.AsSpan(array.Offset, array.Count * array.Size);
    static ReadOnlySpan<byte> EncodingInput(object input) => input switch
    {
        BinaryBuffer buffer => buffer.Bytes,
        BinaryView view => view.OutOfBounds ? ReadOnlySpan<byte>.Empty : view.Buffer.Bytes.AsSpan(view.Offset, view.Length),
        TypedArrayValue array => TypedArrayBytes(array),
        _ when IsUndefined(input) => ReadOnlySpan<byte>.Empty,
        _ => throw new InvalidOperationException("TextDecoder input must be an ArrayBuffer or a view")
    };
    static TextEncoderValue RequireTextEncoder(object receiver) => receiver as TextEncoderValue ?? throw new InvalidOperationException("TextEncoder receiver required");
    static TextDecoderValue RequireTextDecoder(object receiver) => receiver as TextDecoderValue ?? throw new InvalidOperationException("TextDecoder receiver required");
    static string EncoderText(object value) => IsUndefined(value) ? "" : StringValue(value);
    sealed class TextEncoderConstructor : NativeNamespaceFunction
    {
        internal TextEncoderConstructor() : base("TextEncoder", static _ => throw new InvalidOperationException("TextEncoder requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "TextEncoder");
            prototype.DefineClassAccessor("encoding", new CollectionMethod("get encoding", 0, (receiver, _) => { RequireTextEncoder(receiver); return "utf-8"; }), 1);
            prototype.DefineMethod("encode", new CollectionMethod("encode", 0, (receiver, args) =>
            {
                RequireTextEncoder(receiver);
                string text = EncoderText(FunctionArgument(args, 0));
                var result = AllocateTypedArray("Uint8Array", Encoding.UTF8.GetByteCount(text));
                Encoding.UTF8.GetBytes(text, result.Buffer.Bytes);
                return result;
            }));
            prototype.DefineMethod("encodeInto", new CollectionMethod("encodeInto", 2, (receiver, args) =>
            {
                RequireTextEncoder(receiver);
                string text = StringValue(FunctionArgument(args, 0));
                if (FunctionArgument(args, 1) is not TypedArrayValue { Name: "Uint8Array" } destination)
                    throw new InvalidOperationException("TextEncoder destination must be a Uint8Array");
                Span<byte> bytes = TypedArrayBytes(destination);
                int read = 0, written = 0;
                while (read < text.Length)
                {
                    var status = Rune.DecodeFromUtf16(text.AsSpan(read), out var rune, out int consumed);
                    if (status != OperationStatus.Done) { rune = Rune.ReplacementChar; consumed = 1; }
                    if (!rune.TryEncodeToUtf8(bytes[written..], out int count)) break;
                    written += count; read += consumed;
                }
                var result = new JsDynamicObject();
                result.DefineOwn("read", (double)read); result.DefineOwn("written", (double)written);
                return result;
            }));
        }
        public override object Construct(object[] args) => new TextEncoderValue();
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
    static TextDecoderValue NewTextDecoder(object[] args)
    {
        object input = FunctionArgument(args, 0);
        string label = IsUndefined(input) ? "utf-8" : StringValue(input).Trim(' ', '\t', '\r', '\n', '\f').ToLowerInvariant();
        string name = label switch
        {
            "utf-8" or "utf8" or "unicode-1-1-utf-8" or "unicode11utf8" or "unicode20utf8" or "x-unicode20utf8" => "utf-8",
            "utf-16" or "utf-16le" or "ucs-2" or "unicode" or "unicodefeff" or "csunicode" or "iso-10646-ucs-2" => "utf-16le",
            "utf-16be" or "unicodefffe" => "utf-16be",
            "ansi_x3.4-1968" or "cp819" or "csisolatin1" or "ibm819" or "iso-ir-100" or "iso88591" or "iso_8859-1" or "iso_8859-1:1987" or "l1" or "x-cp1252" or "windows-1252" or "cp1252" or "latin1" or "iso-8859-1" or "iso8859-1" or "ascii" or "us-ascii" => "windows-1252",
            _ => LegacyDecoderEncoding(label).Name
        };
        object options = FunctionArgument(args, 1);
        if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("TextDecoder options must be an object");
        bool fatal = options is TsObject obj && Truthy(obj.Get("fatal"));
        bool ignoreBom = options is TsObject other && Truthy(other.Get("ignoreBOM"));
        Encoding encoding;
        if (name == "utf-8") encoding = new UTF8Encoding(false, fatal);
        // Malformed UTF-16 deliberately follows CLR replacement grouping.
        else if (name is "utf-16le" or "utf-16be") encoding = new UnicodeEncoding(name == "utf-16be", false, fatal);
        else
        {
            encoding = CodePagesEncodingProvider.Instance.GetEncoding(name == "windows-1252" ? 1252 : LegacyDecoderEncoding(name).CodePage)
                ?? throw new ArgumentOutOfRangeException(null, "TextDecoder encoding is unavailable on this platform");
            // Legacy mapping tables and malformed-sequence grouping follow the CLR provider.
            encoding = (Encoding)encoding.Clone();
            encoding.DecoderFallback = fatal ? DecoderFallback.ExceptionFallback : new DecoderReplacementFallback("\ufffd");
        }
        return new TextDecoderValue(name, encoding, fatal, ignoreBom);
    }
    sealed class TextDecoderConstructor : NativeNamespaceFunction
    {
        internal TextDecoderConstructor() : base("TextDecoder", static _ => throw new InvalidOperationException("TextDecoder requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "TextDecoder");
            prototype.DefineClassAccessor("encoding", new CollectionMethod("get encoding", 0, (receiver, _) => RequireTextDecoder(receiver).Name), 1);
            prototype.DefineClassAccessor("fatal", new CollectionMethod("get fatal", 0, (receiver, _) => BoxBoolean(RequireTextDecoder(receiver).Fatal)), 1);
            prototype.DefineClassAccessor("ignoreBOM", new CollectionMethod("get ignoreBOM", 0, (receiver, _) => BoxBoolean(RequireTextDecoder(receiver).IgnoreBom)), 1);
            prototype.DefineMethod("decode", new CollectionMethod("decode", 0, (receiver, args) =>
            {
                var decoder = RequireTextDecoder(receiver);
                object input = FunctionArgument(args, 0), options = FunctionArgument(args, 1);
                if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("TextDecoder options must be an object");
                bool stream = options is TsObject obj && Truthy(obj.Get("stream"));
                return decoder.Decode(EncodingInput(input), stream);
            }));
        }
        public override object Construct(object[] args) => NewTextDecoder(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
