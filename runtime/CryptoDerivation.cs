using System;
using System.Security.Cryptography;

public static partial class Js
{
    static object KdfImport(object[] args, string name)
    {
        if (StringValue(FunctionArgument(args, 0)) != "raw") throw CryptoError("NotSupportedError", "Derivation base keys require raw import");
        byte[] bytes = CryptoBytes(FunctionArgument(args, 1));
        if (Truthy(FunctionArgument(args, 3))) throw CryptoError("SyntaxError", "Derivation base keys cannot be extractable");
        string[] usages = SecretKeyUsages(FunctionArgument(args, 4), new[] { "deriveBits", "deriveKey" });
        return new CryptoKeyValue(bytes, null, false, usages, name);
    }
    static HashAlgorithmName DerivationHash(object value) => CryptoAlgorithm(value) switch {
        "SHA-1" => HashAlgorithmName.SHA1, "SHA-256" => HashAlgorithmName.SHA256,
        "SHA-384" => HashAlgorithmName.SHA384, "SHA-512" => HashAlgorithmName.SHA512,
        _ => throw CryptoError("NotSupportedError", "Unsupported key derivation hash")
    };
    static int DerivationInteger(object value, string name)
    {
        double number = Math.Truncate(Number(value));
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue) throw new InvalidOperationException($"Invalid {name}");
        if (number > int.MaxValue) throw CryptoError("OperationError", $"This platform requires {name} to fit a signed 32-bit integer");
        return (int)number;
    }
    static object CryptoDeriveBits(object[] args, string usage = "deriveBits")
    {
        object algorithm = FunctionArgument(args, 0);
        string name = CryptoAlgorithm(algorithm);
        if (name is not ("PBKDF2" or "HKDF")) throw CryptoError("NotSupportedError", "Unsupported key derivation algorithm");
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Name != name || !Array.Exists(key.Usages, value => value == usage)) throw CryptoError("InvalidAccessError", "Key does not permit this derivation");
        if (algorithm is not TsObject options) throw new InvalidOperationException("Key derivation requires an options object");
        object hashValue = options.Get("hash"); if (IsUndefined(hashValue)) throw new InvalidOperationException("Key derivation requires a hash");
        var hash = DerivationHash(hashValue); byte[] salt = CryptoBytes(options.Get("salt"));
        object length = FunctionArgument(args, 2);
        if (IsNullish(length)) throw CryptoError("OperationError", "Key derivation requires an output length");
        uint requestedBits = unchecked((uint)ToInt32(Number(length)));
        if (requestedBits > int.MaxValue) throw CryptoError("OperationError", "This platform requires output length to fit a signed 32-bit integer");
        int bits = (int)requestedBits;
        if (bits % 8 != 0) throw CryptoError("OperationError", "Key derivation requires a whole-byte output length");
        byte[] bytes;
        if (name == "PBKDF2") {
            int iterations = DerivationInteger(options.Get("iterations"), "iterations");
            if (iterations == 0) throw CryptoError("OperationError", "PBKDF2 requires positive iterations");
            bytes = bits == 0 ? Array.Empty<byte>() : Rfc2898DeriveBytes.Pbkdf2(key.Bytes.AsSpan(), salt.AsSpan(), iterations, hash, bits / 8);
        } else {
            byte[] info = CryptoBytes(options.Get("info"));
            int hashBytes = hash == HashAlgorithmName.SHA1 ? 20 : hash == HashAlgorithmName.SHA256 ? 32 : hash == HashAlgorithmName.SHA384 ? 48 : 64;
            if (bits / 8 > 255 * hashBytes) throw CryptoError("OperationError", "HKDF output exceeds the hash algorithm limit");
            bytes = bits == 0 ? Array.Empty<byte>() : HKDF.DeriveKey(hash, key.Bytes, bits / 8, salt, info);
        }
        return new BinaryBuffer(0) { Bytes = bytes };
    }
    static object CryptoDeriveKey(object[] args)
    {
        object target = FunctionArgument(args, 2); string name = CryptoAlgorithm(target); int bits;
        if (name is "AES-GCM" or "AES-CBC" or "AES-CTR" or "AES-KW") {
            if (target is not TsObject options) throw new InvalidOperationException("AES derivation requires a key length");
            object length = options.Get("length");
            if (IsUndefined(length)) throw new InvalidOperationException("AES derivation requires a key length");
            bits = DerivationInteger(length, "key length");
            if (bits is not (128 or 192 or 256)) throw CryptoError("OperationError", "AES keys require 128, 192, or 256 bits");
        } else if (name == "HMAC") {
            string hash = HmacHash(target); bits = HmacLength(target, hash is "SHA-384" or "SHA-512" ? 1024 : 512);
        } else if (name is "PBKDF2" or "HKDF") throw CryptoError("OperationError", "Derivation base keys have no fixed key length");
        else throw CryptoError("NotSupportedError", "Unsupported derived key algorithm");
        var bytes = (BinaryBuffer)CryptoDeriveBits(new object[] { FunctionArgument(args, 0), FunctionArgument(args, 1), (double)bits }, "deriveKey");
        try { return CryptoImport(new object[] { "raw", bytes, target, FunctionArgument(args, 3), FunctionArgument(args, 4) }); }
        finally { CryptographicOperations.ZeroMemory(bytes.Bytes); }
    }
}
