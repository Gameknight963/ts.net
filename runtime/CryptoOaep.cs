using System;
using System.Security.Cryptography;

public static partial class Js
{
    static object RsaCrypt(object[] args, bool decrypt, string usage)
    {
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Name != "RSA-OAEP" || key.Rsa == null || key.PrivateKey != decrypt ||
            Array.IndexOf(key.Usages, usage ?? (decrypt ? "decrypt" : "encrypt")) < 0)
            throw CryptoError("InvalidAccessError", "Key does not permit RSA-OAEP operation");
        object label = FunctionArgument(args, 0) is TsObject options ? options.Get("label") : Undefined;
        if (!IsUndefined(label) && CryptoBytes(label).Length != 0)
            throw CryptoError("NotSupportedError", "RSA-OAEP currently requires an empty label");
        byte[] input = CryptoBytes(FunctionArgument(args, 2));
        try {
            var padding = RSAEncryptionPadding.CreateOaep(DerivationHash(key.Hash));
            return new BinaryBuffer(0) { Bytes = decrypt ? key.Rsa.Decrypt(input, padding) : key.Rsa.Encrypt(input, padding) };
        } catch (CryptographicException) { throw CryptoError("OperationError", "RSA-OAEP operation failed"); }
        finally { CryptographicOperations.ZeroMemory(input); }
    }
}
