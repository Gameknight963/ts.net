using System;
using System.Collections.Generic;
using System.Security.Cryptography;

public static partial class Js
{
    sealed class CryptoKeyValue(byte[] bytes, string hash, bool extractable, string[] usages, string name = "HMAC", ECDsa ec = null, string curve = null, bool privateKey = false) : TsObject
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly ECDsa Ec = ec;
        internal readonly string Curve = curve;
        internal readonly bool PrivateKey = privateKey;
        internal string KeyType => Ec == null ? "secret" : PrivateKey ? "private" : "public";
        internal readonly string Name = name;
        internal readonly string Hash = hash;
        internal readonly bool Extractable = extractable;
        internal readonly string[] Usages = usages;
        TsObject algorithm;
        JsArray publicUsages;
        internal TsObject Algorithm {
            get {
                if (algorithm != null) return algorithm;
                var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
                result.Set("name", Name);
                if (Ec != null) result.Set("namedCurve", Curve);
                if (Name == "HMAC") {
                    var hash = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; hash.Set("name", Hash); result.Set("hash", hash);
                }
                if (Name is "HMAC" or "AES-GCM" or "AES-CBC" or "AES-CTR" or "AES-KW") result.Set("length", (double)Bytes.Length * 8);
                return algorithm = result;
            }
        }
        internal JsArray PublicUsages {
            get {
                if (publicUsages != null) return publicUsages;
                var result = new JsArray(0); foreach (string usage in Usages) result.Append(usage); return publicUsages = result;
            }
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)CryptoKeys.Constructor.Get("prototype");
    }
    static Exception CryptoError(string name, string message) => MakeThrown(new DomExceptionValue(message, name));
    static CryptoKeyValue RequireCryptoKey(object value) => value as CryptoKeyValue ?? throw new InvalidOperationException("CryptoKey required");
    static string CryptoAlgorithm(object value)
    {
        if (value is TsObject options) {
            value = options.Get("name");
            if (IsUndefined(value)) throw new InvalidOperationException("Algorithm requires a name");
        }
        return StringValue(value).ToUpperInvariant();
    }
    static string HmacHash(object algorithm)
    {
        if (algorithm is not TsObject options) throw new InvalidOperationException("HMAC requires a hash");
        object value = options.Get("hash");
        if (IsUndefined(value)) throw new InvalidOperationException("HMAC requires a hash");
        string hash = CryptoAlgorithm(value);
        if (hash is not ("SHA-1" or "SHA-256" or "SHA-384" or "SHA-512")) throw CryptoError("NotSupportedError", "Unsupported HMAC hash");
        return hash;
    }
    static byte[] CryptoBytes(object value)
    {
        if (value is not (BinaryBuffer or BinaryView or TypedArrayValue)) throw new InvalidOperationException("Buffer source required");
        if (value is BinaryBuffer buffer && buffer.Detached || value is BinaryView view && view.OutOfBounds || value is TypedArrayValue array && array.OutOfBounds) throw new InvalidOperationException("Buffer is detached or out of bounds");
        return EncodingInput(value).ToArray();
    }
    static string[] SecretKeyUsages(object value, string[] allowed, bool unique = false)
    {
        var result = new List<string>(); object iterator = Iteration.Start(value); int position = 0;
        try {
            while (position < Iteration.Length(iterator)) {
                string usage = StringValue(Iteration.Value(iterator, position));
                if (Array.IndexOf(allowed, usage) < 0) throw CryptoError("SyntaxError", "Invalid secret key usage");
                if (!unique || !result.Contains(usage)) result.Add(usage);
                position += Iteration.Step(iterator, position);
            }
        } catch { CloseIterationAt(iterator, position, true); throw; }
        if (result.Count == 0) throw CryptoError("SyntaxError", "A secret key requires at least one usage");
        return result.ToArray();
    }
    static int HmacLength(object algorithm, int fallback)
    {
        object value = algorithm is TsObject options ? options.Get("length") : Undefined;
        if (IsUndefined(value)) return fallback;
        double number = Number(value);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue) throw new InvalidOperationException("Invalid HMAC length");
        number = Math.Truncate(number);
        if (number > int.MaxValue) throw CryptoError("OperationError", "HMAC key is too large");
        return (int)number;
    }
    static object CryptoKeyOperation(object receiver, Func<object> operation)
    {
        var promise = new JsPromise();
        try { RequireCrypto(receiver, true); promise.Settle(operation(), false); }
        catch (Exception error) { promise.Settle(CaughtValue(error), true); }
        return promise;
    }
    static object CryptoImport(object[] args) => CryptoAlgorithm(FunctionArgument(args, 2)) switch {
        "ECDSA" => EcImport(args), "HMAC" => HmacImport(args), "AES-GCM" => AesImport(args), "AES-CBC" => AesImport(args, "AES-CBC"), "AES-CTR" => AesImport(args, "AES-CTR"), "AES-KW" => AesImport(args, "AES-KW"), "PBKDF2" => KdfImport(args, "PBKDF2"), "HKDF" => KdfImport(args, "HKDF"),
        _ => throw CryptoError("NotSupportedError", "Unsupported key algorithm")
    };
    static object CryptoGenerate(object[] args) => CryptoAlgorithm(FunctionArgument(args, 0)) switch {
        "ECDSA" => EcGenerate(args), "HMAC" => HmacGenerate(args), "AES-GCM" => AesGenerate(args), "AES-CBC" => AesGenerate(args, "AES-CBC"), "AES-CTR" => AesGenerate(args, "AES-CTR"), "AES-KW" => AesGenerate(args, "AES-KW"),
        _ => throw CryptoError("NotSupportedError", "Unsupported key algorithm")
    };
    static object HmacImport(object[] args)
    {
        string format = StringValue(FunctionArgument(args, 0)); object input = FunctionArgument(args, 1);
        if (format is not ("raw" or "jwk")) throw CryptoError("NotSupportedError", "Unsupported HMAC key format");
        byte[] rawBytes = format == "raw" ? CryptoBytes(input) : null;
        object algorithm = FunctionArgument(args, 2);
        string hash = HmacHash(algorithm); string[] usages = SecretKeyUsages(FunctionArgument(args, 4), new[] { "sign", "verify" });
        bool extractable = Truthy(FunctionArgument(args, 3));
        byte[] bytes = format == "raw" ? rawBytes : SecretJwkImport(input, "sig", _ => HmacJwkAlgorithm(hash), extractable, usages);
        int bits = HmacLength(algorithm, checked(bytes.Length * 8));
        if (bits % 8 != 0) throw CryptoError("NotSupportedError", "HMAC keys require whole bytes");
        if (bits == 0 || bits != (long)bytes.Length * 8) throw CryptoError("DataError", "HMAC key length does not match its bytes");
        return new CryptoKeyValue(bytes, hash, extractable, usages);
    }
    static object HmacGenerate(object[] args)
    {
        object algorithm = FunctionArgument(args, 0); string hash = HmacHash(algorithm);
        string[] usages = SecretKeyUsages(FunctionArgument(args, 2), new[] { "sign", "verify" }, true);
        int bits = HmacLength(algorithm, hash is "SHA-384" or "SHA-512" ? 1024 : 512);
        if (bits == 0) throw CryptoError("OperationError", "HMAC length must be positive");
        if (bits % 8 != 0) throw CryptoError("NotSupportedError", "HMAC keys require whole bytes");
        return new CryptoKeyValue(RandomNumberGenerator.GetBytes(bits / 8), hash, Truthy(FunctionArgument(args, 1)), usages);
    }
    static object CryptoExport(object[] args)
    {
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Name is "PBKDF2" or "HKDF") throw CryptoError("NotSupportedError", "Derivation base keys cannot be exported");
        if (!key.Extractable) throw CryptoError("InvalidAccessException", "Key is not extractable");
        string format = StringValue(FunctionArgument(args, 0));
        if (key.Ec != null) return EcExport(key, format);
        if (format == "jwk") {
            var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            result.Set("key_ops", new JsArray(Array.ConvertAll(key.Usages, static usage => (object)usage)));
            result.Set("ext", BoxBoolean(key.Extractable)); result.Set("kty", "oct");
            result.Set("k", Convert.ToBase64String(key.Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'));
            result.Set("alg", key.Name == "HMAC" ? HmacJwkAlgorithm(key.Hash) : AesJwkAlgorithm(key.Bytes.Length, key.Name)); return result;
        }
        if (format != "raw") throw CryptoError("NotSupportedError", "Unsupported HMAC key format");
        var buffer = new BinaryBuffer(key.Bytes.Length); key.Bytes.CopyTo(buffer.Bytes, 0); return buffer;
    }
    static string HmacJwkAlgorithm(string hash) => hash switch { "SHA-1" => "HS1", "SHA-256" => "HS256", "SHA-384" => "HS384", "SHA-512" => "HS512", _ => throw CryptoError("NotSupportedError", "Unsupported HMAC hash") };
    static byte[] SecretJwkImport(object value, string keyUse, Func<int, string> keyAlgorithm, bool extractable, string[] usages)
    {
        if (value is not TsObject jwk) throw new InvalidOperationException("JWK must be an object");
        object type = jwk.Get("kty"), encoded = jwk.Get("k");
        if (IsUndefined(type) || StringValue(type) != "oct" || IsUndefined(encoded)) throw CryptoError("DataError", "JWK requires octet key material");
        string text = StringValue(encoded).Replace('-', '+').Replace('_', '/');
        if (text.Length % 4 == 1) throw CryptoError("DataError", "Invalid JWK key encoding");
        text = text.PadRight((text.Length + 3) / 4 * 4, '=');
        byte[] bytes;
        try { bytes = Convert.FromBase64String(text); }
        catch (FormatException) { throw CryptoError("DataError", "Invalid JWK key encoding"); }
        object algorithm = jwk.Get("alg"), use = jwk.Get("use"), extract = jwk.Get("ext");
        if (!IsUndefined(algorithm) && StringValue(algorithm) != keyAlgorithm(bytes.Length)) throw CryptoError("DataError", "JWK hash does not match the algorithm");
        if (!IsUndefined(use) && StringValue(use) != keyUse) throw CryptoError("DataError", "JWK does not permit signatures");
        if (!IsUndefined(extract) && !Truthy(extract) && extractable) throw CryptoError("DataError", "JWK does not permit extraction");
        object operations = jwk.Get("key_ops");
        if (!IsUndefined(operations)) {
            var allowed = new HashSet<string>(StringComparer.Ordinal); object iterator = Iteration.Start(operations); int position = 0;
            try {
                while (position < Iteration.Length(iterator)) {
                    if (!allowed.Add(StringValue(Iteration.Value(iterator, position)))) throw CryptoError("DataError", "Duplicate JWK key operation");
                    position += Iteration.Step(iterator, position);
                }
            } catch { CloseIterationAt(iterator, position, true); throw; }
            foreach (string usage in usages) if (!allowed.Contains(usage)) throw CryptoError("DataError", "JWK does not permit the requested usage");
        }
        return bytes;
    }
    static byte[] HmacSignature(CryptoKeyValue key, byte[] data) => key.Hash switch {
        "SHA-1" => HMACSHA1.HashData(key.Bytes, data), "SHA-256" => HMACSHA256.HashData(key.Bytes, data),
        "SHA-384" => HMACSHA384.HashData(key.Bytes, data), "SHA-512" => HMACSHA512.HashData(key.Bytes, data),
        _ => throw CryptoError("NotSupportedError", "Unsupported HMAC hash")
    };
    static object HmacSign(object[] args, bool verify)
    {
        string algorithm = CryptoAlgorithm(FunctionArgument(args, 0)); var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (algorithm == "ECDSA") return EcSign(args, verify);
        if (algorithm != "HMAC") throw CryptoError("NotSupportedError", "Unsupported signing algorithm");
        if (key.Name != "HMAC" || !Array.Exists(key.Usages, usage => usage == (verify ? "verify" : "sign"))) throw CryptoError("InvalidAccessError", "Key does not permit this operation");
        byte[] supplied = verify ? CryptoBytes(FunctionArgument(args, 2)) : null;
        byte[] signature = HmacSignature(key, CryptoBytes(FunctionArgument(args, verify ? 3 : 2)));
        if (verify) return BoxBoolean(CryptographicOperations.FixedTimeEquals(signature, supplied));
        var result = new BinaryBuffer(signature.Length); signature.CopyTo(result.Bytes, 0); return result;
    }
    sealed class CryptoKeyConstructor : NativeNamespaceFunction
    {
        internal CryptoKeyConstructor() : base("CryptoKey", static _ => throw new InvalidOperationException("CryptoKey requires host construction"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "CryptoKey");
            prototype.DefineClassAccessor("type", new CollectionMethod("get type", 0, static (receiver, _) => RequireCryptoKey(receiver).KeyType), 1);
            prototype.DefineClassAccessor("extractable", new CollectionMethod("get extractable", 0, static (receiver, _) => BoxBoolean(RequireCryptoKey(receiver).Extractable)), 1);
            prototype.DefineClassAccessor("algorithm", new CollectionMethod("get algorithm", 0, static (receiver, _) => RequireCryptoKey(receiver).Algorithm), 1);
            prototype.DefineClassAccessor("usages", new CollectionMethod("get usages", 0, static (receiver, _) => RequireCryptoKey(receiver).PublicUsages), 1);
        }
        public override object Construct(object[] args) => throw new InvalidOperationException("CryptoKey requires host construction");
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) => throw new InvalidOperationException("CryptoKey requires host construction");
    }
    static class CryptoKeys { internal static readonly JsFunction Constructor = new CryptoKeyConstructor(); }
}
