using System;
using System.Security.Cryptography;

public static partial class Js
{
    static object AesCbc(CryptoKeyValue key, byte[] iv, byte[] input, bool decrypt)
    {
        if (iv.Length != 16) throw CryptoError("OperationError", "AES-CBC requires a 16-byte IV");
        try {
            using var aes = Aes.Create(); aes.Key = key.Bytes;
            byte[] bytes = decrypt ? aes.DecryptCbc(input, iv, PaddingMode.PKCS7) : aes.EncryptCbc(input, iv, PaddingMode.PKCS7);
            return new BinaryBuffer(0) { Bytes = bytes };
        } catch (CryptographicException) { throw CryptoError("OperationError", "AES-CBC encryption or padding validation failed"); }
    }
}
