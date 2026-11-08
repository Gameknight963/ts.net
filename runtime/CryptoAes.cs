using System;
using System.Security.Cryptography;

public static partial class Js
{
    static string AesJwkAlgorithm(int bytes, string name = "AES-GCM") => $"A{bytes * 8}{name[4..]}";
    static string[] AesUsages(object value, bool unique = false) => SecretKeyUsages(value, new[] { "encrypt", "decrypt", "wrapKey", "unwrapKey" }, unique);
    static object AesImport(object[] args, string name = "AES-GCM")
    {
        string format = StringValue(FunctionArgument(args, 0)); object input = FunctionArgument(args, 1);
        string[] usages = AesUsages(FunctionArgument(args, 4)); bool extractable = Truthy(FunctionArgument(args, 3));
        byte[] bytes;
        if (format == "raw") bytes = CryptoBytes(input);
        else if (format == "jwk") bytes = SecretJwkImport(input, "enc", bytes => AesJwkAlgorithm(bytes, name), extractable, usages);
        else throw CryptoError("NotSupportedError", "Unsupported AES key format");
        if (bytes.Length is not (16 or 24 or 32)) throw CryptoError("DataError", "AES keys require 128, 192, or 256 bits");
        return new CryptoKeyValue(bytes, null, extractable, usages, name);
    }
    static object AesGenerate(object[] args, string name = "AES-GCM")
    {
        if (FunctionArgument(args, 0) is not TsObject options) throw new InvalidOperationException("AES generation requires a length");
        object length = options.Get("length"); if (IsUndefined(length)) throw new InvalidOperationException("AES generation requires a length");
        double bits = Math.Truncate(Number(length));
        if (bits is not (128 or 192 or 256)) throw CryptoError("OperationError", "AES keys require 128, 192, or 256 bits");
        string[] usages = AesUsages(FunctionArgument(args, 2), true);
        return new CryptoKeyValue(RandomNumberGenerator.GetBytes((int)bits / 8), null, Truthy(FunctionArgument(args, 1)), usages, name);
    }
    static object AesCrypt(object[] args, bool decrypt, string usage = null)
    {
        object algorithm = FunctionArgument(args, 0);
        string name = CryptoAlgorithm(algorithm);
        if (name is not ("AES-GCM" or "AES-CBC" or "AES-CTR")) throw CryptoError("NotSupportedError", "Unsupported encryption algorithm");
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        string requiredUsage = usage ?? (decrypt ? "decrypt" : "encrypt");
        if (key.Name != name || !Array.Exists(key.Usages, value => value == requiredUsage)) throw CryptoError("InvalidAccessError", "Key does not permit this operation");
        if (algorithm is not TsObject options) throw new InvalidOperationException("AES encryption requires an IV");
        if (name == "AES-CTR") return AesCtr(key, options, CryptoBytes(FunctionArgument(args, 2)));
        byte[] iv = CryptoBytes(options.Get("iv"));
        if (name == "AES-CBC") return AesCbc(key, iv, CryptoBytes(FunctionArgument(args, 2)), decrypt);
        if (iv.Length != 12) throw CryptoError("OperationError", "This platform requires a 12-byte AES-GCM IV");
        object tag = options.Get("tagLength"), additional = options.Get("additionalData");
        double bits = IsUndefined(tag) ? 128 : Math.Truncate(Number(tag));
        if (bits is not (96 or 104 or 112 or 120 or 128)) throw CryptoError("OperationError", "This platform requires a 96-128 bit AES-GCM tag");
        int tagSize = (int)bits / 8;
        if (!AesGcm.IsSupported) throw CryptoError("NotSupportedError", "AES-GCM is unavailable on this platform");
        var supportedTags = AesGcm.TagByteSizes;
        if (tagSize < supportedTags.MinSize || tagSize > supportedTags.MaxSize || supportedTags.SkipSize > 0 && (tagSize - supportedTags.MinSize) % supportedTags.SkipSize != 0)
            throw CryptoError("NotSupportedError", "The requested AES-GCM tag size is unavailable on this platform");
        byte[] aad = IsUndefined(additional) ? Array.Empty<byte>() : CryptoBytes(additional);
        byte[] input = CryptoBytes(FunctionArgument(args, 2));
        if (decrypt && input.Length < tagSize) throw CryptoError("OperationError", "Ciphertext is shorter than its authentication tag");
        var result = new BinaryBuffer(decrypt ? input.Length - tagSize : checked(input.Length + tagSize));
        try {
            using var aes = new AesGcm(key.Bytes, tagSize);
            if (decrypt) aes.Decrypt(iv, input.AsSpan(0, input.Length - tagSize), input.AsSpan(input.Length - tagSize), result.Bytes, aad);
            else aes.Encrypt(iv, input, result.Bytes.AsSpan(0, input.Length), result.Bytes.AsSpan(input.Length), aad);
        } catch (CryptographicException) { CryptographicOperations.ZeroMemory(result.Bytes); throw CryptoError("OperationError", "AES-GCM authentication or encryption failed"); }
        return result;
    }
}
