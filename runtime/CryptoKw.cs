using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

public static partial class Js
{
    static object WrapCrypt(object[] args, bool decrypt) => CryptoAlgorithm(FunctionArgument(args,0)) == "AES-KW"
        ? AesKw(args,decrypt) : AesCrypt(args,decrypt,decrypt ? "unwrapKey" : "wrapKey");
    static object AesKw(object[] args, bool decrypt)
    {
        var key = RequireCryptoKey(FunctionArgument(args,1));
        string usage = decrypt ? "unwrapKey" : "wrapKey";
        if (key.Name != "AES-KW" || !Array.Exists(key.Usages,value => value == usage))
            throw CryptoError("InvalidAccessError","Key does not permit this operation");
        byte[] input = CryptoBytes(FunctionArgument(args,2)), block = new byte[16];
        BinaryBuffer result = null;
        try {
            if (input.Length % 8 != 0 || input.Length < (decrypt ? 24 : 16))
                throw CryptoError("OperationError","AES-KW requires at least two whole 64-bit key blocks");
            result = new BinaryBuffer(decrypt ? input.Length - 8 : checked(input.Length + 8));
            int offset = decrypt ? 0 : 8, count = (decrypt ? result.Bytes.Length : input.Length) / 8;
            input.AsSpan(decrypt ? 8 : 0).CopyTo(result.Bytes.AsSpan(offset));
            ulong register = decrypt ? BinaryPrimitives.ReadUInt64BigEndian(input) : 0xa6a6a6a6a6a6a6a6UL;
            using var aes = Aes.Create(); aes.Key = key.Bytes; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var transform = decrypt ? aes.CreateDecryptor() : aes.CreateEncryptor();
            for (int round = decrypt ? 5 : 0; decrypt ? round >= 0 : round < 6; round += decrypt ? -1 : 1) {
                for (int index = decrypt ? count : 1; decrypt ? index >= 1 : index <= count; index += decrypt ? -1 : 1) {
                    ulong step = (ulong)count * (uint)round + (uint)index;
                    BinaryPrimitives.WriteUInt64BigEndian(block,decrypt ? register ^ step : register);
                    result.Bytes.AsSpan(offset + (index - 1) * 8,8).CopyTo(block.AsSpan(8));
                    transform.TransformBlock(block,0,16,block,0);
                    register = BinaryPrimitives.ReadUInt64BigEndian(block);
                    if (!decrypt) register ^= step;
                    block.AsSpan(8).CopyTo(result.Bytes.AsSpan(offset + (index - 1) * 8));
                }
            }
            if (decrypt) {
                Span<byte> expected = stackalloc byte[8]; expected.Fill(0xa6);
                BinaryPrimitives.WriteUInt64BigEndian(block,register);
                if (!CryptographicOperations.FixedTimeEquals(block.AsSpan(0,8),expected))
                    throw CryptoError("OperationError","AES-KW integrity check failed");
            } else BinaryPrimitives.WriteUInt64BigEndian(result.Bytes,register);
            return result;
        }
        catch (CryptographicException) {if (result != null) CryptographicOperations.ZeroMemory(result.Bytes); throw CryptoError("OperationError","AES-KW operation failed");}
        catch {if (result != null) CryptographicOperations.ZeroMemory(result.Bytes);throw;}
        finally {CryptographicOperations.ZeroMemory(block); CryptographicOperations.ZeroMemory(input);}
    }
}
