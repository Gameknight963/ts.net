using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

public static partial class Js
{
    static object AesCtr(CryptoKeyValue key, TsObject options, byte[] input)
    {
        byte[] initial = CryptoBytes(options.Get("counter"));
        object length = options.Get("length");
        if (IsUndefined(length)) throw new InvalidOperationException("AES-CTR requires a counter length");
        double requested = Math.Truncate(Number(length));
        if (!double.IsFinite(requested) || requested < 0 || requested > 255)
            throw new InvalidOperationException("AES-CTR counter length must fit an unsigned byte");
        int bits = (int)requested;
        if (initial.Length != 16 || bits is < 1 or > 128)
            throw CryptoError("OperationError", "AES-CTR requires a 16-byte counter and 1-128 counter bits");
        long blocks = ((long)input.Length + 15) / 16;
        if (bits < 31 && blocks > (1L << bits))
            throw CryptoError("OperationError", "AES-CTR input would repeat a counter");

        var result = new BinaryBuffer(input.Length);
        if (input.Length == 0) return result;
        // Encrypt batches of counters through the platform AES implementation.
        // Only the low 'bits' advance; the remaining nonce bits never change.
        UInt128 mask = bits == 128 ? UInt128.MaxValue : ((UInt128)1 << bits) - 1;
        UInt128 counter = BinaryPrimitives.ReadUInt128BigEndian(initial);
        UInt128 nonce = counter & ~mask;
        byte[] counters = new byte[4096], stream = new byte[4096];
        try {
            using var aes = Aes.Create();
            aes.Key = key.Bytes; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var encryptor = aes.CreateEncryptor();
            for (int offset = 0; offset < input.Length;) {
                int count = Math.Min(counters.Length, input.Length - offset);
                int padded = (count + 15) / 16 * 16;
                for (int block = 0; block < padded; block += 16) {
                    BinaryPrimitives.WriteUInt128BigEndian(counters.AsSpan(block, 16), counter);
                    counter = nonce | (unchecked(counter + 1) & mask);
                }
                encryptor.TransformBlock(counters, 0, padded, stream, 0);
                for (int index = 0; index < count; index++)
                    result.Bytes[offset + index] = (byte)(input[offset + index] ^ stream[index]);
                offset += count;
            }
        } catch (CryptographicException) {
            CryptographicOperations.ZeroMemory(result.Bytes);
            throw CryptoError("OperationError", "AES-CTR encryption failed");
        } finally {
            CryptographicOperations.ZeroMemory(counters);
            CryptographicOperations.ZeroMemory(stream);
        }
        return result;
    }
}
