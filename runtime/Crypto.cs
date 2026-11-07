using System;
using System.Security.Cryptography;

public static partial class Js
{
    sealed class CryptoValue(bool subtle) : TsObject
    {
        internal readonly bool Subtle = subtle;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)(Subtle ? RuntimeNamespaces.SubtleCryptoType : RuntimeNamespaces.CryptoType).Get("prototype");
    }
    static void RequireCrypto(object receiver, bool subtle)
    {
        if (receiver is not CryptoValue value || value.Subtle != subtle) throw new InvalidOperationException("Incompatible crypto receiver");
    }
    static object CryptoRandomValues(object receiver, object input)
    {
        RequireCrypto(receiver, false);
        if (input is not TypedArrayValue array || array.Kind.StartsWith("Float", StringComparison.Ordinal))
            throw MakeThrown(new DomExceptionValue("An integer typed array is required", "TypeMismatchError"));
        int length = array.Count * array.Size;
        if (length > 65536) throw MakeThrown(new DomExceptionValue("Random byte quota exceeded", "QuotaExceededError"));
        RandomNumberGenerator.Fill(array.Buffer.Bytes.AsSpan(array.Offset, length));
        return array;
    }
    static string CryptoRandomUuid(object receiver)
    {
        RequireCrypto(receiver, false);
        Span<byte> bytes = stackalloc byte[16]; RandomNumberGenerator.Fill(bytes);
        bytes[6] = (byte)((bytes[6] & 15) | 64); bytes[8] = (byte)((bytes[8] & 63) | 128);
        return new Guid(bytes, bigEndian: true).ToString("D");
    }
    static object CryptoDigest(object receiver, object[] args)
    {
        var promise = new JsPromise();
        try
        {
            RequireCrypto(receiver, true);
            if (args.Length < 2 || args[1] is not (BinaryBuffer or BinaryView or TypedArrayValue)) throw new InvalidOperationException("Digest requires a buffer source");
            object algorithm = args[0];
            if (algorithm is TsObject options)
            {
                algorithm = options.Get("name");
                if (IsUndefined(algorithm)) throw new InvalidOperationException("Digest algorithm requires a name");
            }
            string name = StringValue(algorithm).ToUpperInvariant();
            var input = EncodingInput(args[1]);
            byte[] hash = name switch
            {
                "SHA-1" => SHA1.HashData(input), "SHA-256" => SHA256.HashData(input),
                "SHA-384" => SHA384.HashData(input), "SHA-512" => SHA512.HashData(input),
                _ => throw MakeThrown(new DomExceptionValue("Unsupported digest algorithm", "NotSupportedError"))
            };
            var buffer = new BinaryBuffer(hash.Length); hash.CopyTo(buffer.Bytes, 0);
            promise.Settle(buffer, false);
        }
        catch (Exception error) { promise.Settle(CaughtValue(error), true); }
        return promise;
    }
    sealed class CryptoConstructorFunction : NativeNamespaceFunction
    {
        internal CryptoConstructorFunction(bool subtle) : base(subtle ? "SubtleCrypto" : "Crypto", static _ => throw new InvalidOperationException("Crypto types require host construction"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), subtle ? "SubtleCrypto" : "Crypto");
            if (subtle) prototype.DefineOwn("digest", new CollectionMethod("digest", 2, CryptoDigest));
            else
            {
                prototype.DefineOwn("getRandomValues", new CollectionMethod("getRandomValues", 1, static (receiver, args) => CryptoRandomValues(receiver, FunctionArgument(args, 0))));
                prototype.DefineOwn("randomUUID", new CollectionMethod("randomUUID", 0, static (receiver, _) => CryptoRandomUuid(receiver)));
                prototype.DefineAccessor("subtle", new CollectionMethod("get subtle", 0, static (receiver, _) => { RequireCrypto(receiver, false); return RuntimeNamespaces.SubtleCrypto; }), 1);
            }
        }
        public override object Construct(object[] args) => throw new InvalidOperationException("Crypto types require host construction");
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) => throw new InvalidOperationException("Crypto types require host construction");
    }
}
