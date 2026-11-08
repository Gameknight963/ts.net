using System;
using System.Security.Cryptography;

public static partial class Js
{
    static object EcDeriveBits(object[] args, string usage)
    {
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Name != "ECDH" || !key.PrivateKey || Array.IndexOf(key.Usages, usage) < 0)
            throw CryptoError("InvalidAccessError", "Key does not permit ECDH derivation");
        if (FunctionArgument(args, 0) is not TsObject options) throw new InvalidOperationException("ECDH requires a public key");
        var other = RequireCryptoKey(options.Get("public"));
        if (other.Name != "ECDH" || other.PrivateKey || other.Curve != key.Curve)
            throw CryptoError("InvalidAccessError", "ECDH requires a public key on the same curve");
        object requested = FunctionArgument(args, 2);
        uint bits = IsNullish(requested) ? (uint)(EcWidth(key.Curve) * 8) : unchecked((uint)ToInt32(Number(requested)));
        if (bits > EcWidth(key.Curve) * 8) throw CryptoError("OperationError", "Requested ECDH output is too long");
        byte[] secret = null;
        try
        {
            secret = key.Agreement.DeriveRawSecretAgreement(other.AgreementPublic);
            int count = (int)((bits + 7) / 8);
            byte[] output = secret.AsSpan(0, count).ToArray();
            if ((bits & 7) != 0) output[^1] &= (byte)(255 << (8 - (int)(bits & 7)));
            return new BinaryBuffer(0) { Bytes = output };
        }
        catch (CryptographicException) { throw CryptoError("OperationError", "ECDH derivation failed"); }
        finally { if (secret != null) CryptographicOperations.ZeroMemory(secret); }
    }
}
