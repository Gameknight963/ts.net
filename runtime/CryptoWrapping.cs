using System;
using System.Security.Cryptography;
using System.Text;

public static partial class Js
{
    static object CryptoWrap(object[] args)
    {
        string format = StringValue(FunctionArgument(args, 0));
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        var wrappingKey = RequireCryptoKey(FunctionArgument(args, 2));
        object algorithm = FunctionArgument(args, 3);
        string name = CryptoAlgorithm(algorithm);
        if (name is not ("AES-GCM" or "AES-CBC" or "AES-CTR" or "AES-KW")) throw CryptoError("NotSupportedError", "Unsupported key wrapping algorithm");
        if (wrappingKey.Name != name || !Array.Exists(wrappingKey.Usages, value => value == "wrapKey")) throw CryptoError("InvalidAccessError", "Key does not permit wrapping");
        object exported = CryptoExport(new object[] { format, key });
        BinaryBuffer bytes;
        if (exported is BinaryBuffer buffer) bytes = buffer;
        else {
            byte[] encoded = Encoding.UTF8.GetBytes((string)new JsonWriter(Undefined, Undefined).Write(exported));
            if (name == "AES-KW" && encoded.Length % 8 != 0) {
                byte[] padded = new byte[checked((encoded.Length + 7) / 8 * 8)];
                padded.AsSpan().Fill(0x20); encoded.CopyTo(padded,0);
                CryptographicOperations.ZeroMemory(encoded); encoded = padded;
            }
            bytes = new BinaryBuffer(0) { Bytes = encoded };
        }
        try { return WrapCrypt(new object[] { algorithm, wrappingKey, bytes }, false); }
        finally { CryptographicOperations.ZeroMemory(bytes.Bytes); }
    }
    static object CryptoUnwrap(object[] args)
    {
        string format = StringValue(FunctionArgument(args, 0));
        var bytes = (BinaryBuffer)WrapCrypt(new object[] { FunctionArgument(args, 3), FunctionArgument(args, 2), FunctionArgument(args, 1) }, true);
        try {
            object input = bytes;
            if (format == "jwk") {
                try { input = new JsonParser(new UTF8Encoding(false, true).GetString(bytes.Bytes), false).Parse().Value; }
                catch (Exception error) when (error is DecoderFallbackException or JsThrownException) { throw CryptoError("DataError", "Wrapped JWK is not valid UTF-8 JSON"); }
            }
            return CryptoImport(new object[] { format, input, FunctionArgument(args, 4), FunctionArgument(args, 5), FunctionArgument(args, 6) });
        } finally { CryptographicOperations.ZeroMemory(bytes.Bytes); }
    }
}
