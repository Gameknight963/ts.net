using System;
using System.Security.Cryptography;

public static partial class Js
{
    const string RsaSigningName = "RSASSA-PKCS1-v1_5";
    static CryptoKeyValue RsaKey(RSA rsa, string hash, bool privateKey, bool extractable, string[] usages) =>
        new(Array.Empty<byte>(), hash, extractable, usages, RsaSigningName, privateKey: privateKey, rsa: rsa);
    static string RsaJwkAlgorithm(string hash) => hash == "SHA-1" ? "RS1" : "RS" + hash[4..];
    static object RsaGenerate(object[] args)
    {
        object algorithm = FunctionArgument(args, 0); string hash = HmacHash(algorithm);
        if (algorithm is not TsObject options) throw new InvalidOperationException("RSA requires generation parameters");
        int bits = DerivationInteger(options.Get("modulusLength"), "RSA modulus length");
        if (options.Get("publicExponent") is not TypedArrayValue { Name: "Uint8Array" } exponent) throw new InvalidOperationException("RSA public exponent requires Uint8Array");
        byte[] encoded = CryptoBytes(exponent); int start = 0;
        while (start < encoded.Length && encoded[start] == 0) start++;
        if (!encoded.AsSpan(start).SequenceEqual(new byte[] { 1, 0, 1 })) throw CryptoError("NotSupportedError", "RSA generation currently requires public exponent 65537");
        string[] usages = AsymmetricKeyUsages(FunctionArgument(args, 2), null, RsaSigningName);
        if (Array.IndexOf(usages, "sign") < 0) throw CryptoError("SyntaxError", "Private key requires a usage");
        RSA privateKey = null, publicKey = null;
        try
        {
            privateKey = RSA.Create(bits); publicKey = RSA.Create(privateKey.ExportParameters(false));
            var result = new JsDynamicObject();
            result.DefineOwn("privateKey", RsaKey(privateKey, hash, true, Truthy(FunctionArgument(args, 1)), new[] { "sign" }));
            result.DefineOwn("publicKey", RsaKey(publicKey, hash, false, true, Array.IndexOf(usages, "verify") < 0 ? Array.Empty<string>() : new[] { "verify" })); return result;
        }
        catch (Exception error) when (error is CryptographicException or ArgumentException)
        {
            privateKey?.Dispose(); publicKey?.Dispose(); throw CryptoError("OperationError", "RSA key generation failed");
        }
        catch { privateKey?.Dispose(); publicKey?.Dispose(); throw; }
    }
    static void ClearRsaPrivate(RSAParameters parameters)
    {
        foreach (byte[] value in new[] { parameters.D, parameters.P, parameters.Q, parameters.DP, parameters.DQ, parameters.InverseQ })
            if (value != null) CryptographicOperations.ZeroMemory(value);
    }
    static byte[] PadRsaPrivate(byte[] value, int length)
    {
        if (value.Length == length) return value;
        try
        {
            if (value.Length > length) throw CryptoError("DataError", "RSA private integer is too long");
            var padded = new byte[length]; value.CopyTo(padded, length - value.Length); return padded;
        }
        finally { CryptographicOperations.ZeroMemory(value); }
    }
    static string EncodeRsaInteger(byte[] value)
    {
        int start = 0; while (start + 1 < value.Length && value[start] == 0) start++;
        return Convert.ToBase64String(value.AsSpan(start)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    static object RsaImport(object[] args)
    {
        string format = StringValue(FunctionArgument(args, 0)), hash = HmacHash(FunctionArgument(args, 2));
        object input = FunctionArgument(args, 1); bool extractable = Truthy(FunctionArgument(args, 3));
        bool privateKey = format == "pkcs8" || format == "jwk" && input is TsObject obj && !IsUndefined(obj.Get("d"));
        string[] usages = AsymmetricKeyUsages(FunctionArgument(args, 4), privateKey, RsaSigningName);
        RSA rsa = RSA.Create(); RSAParameters parameters = default;
        try
        {
            if (format == "jwk")
            {
                if (input is not TsObject jwk) throw new InvalidOperationException("JWK must be an object");
                if (StringValue(jwk.Get("kty")) != "RSA" || !IsUndefined(jwk.Get("oth"))) throw CryptoError("DataError", "Invalid RSA JWK type");
                ValidateJwkPermissions(jwk, RsaJwkAlgorithm(hash), "sig", extractable, usages);
                parameters.Modulus = DecodeJwkInteger(jwk.Get("n")); parameters.Exponent = DecodeJwkInteger(jwk.Get("e"));
                if (privateKey)
                {
                    parameters.D = DecodeJwkInteger(jwk.Get("d")); parameters.P = DecodeJwkInteger(jwk.Get("p")); parameters.Q = DecodeJwkInteger(jwk.Get("q"));
                    parameters.DP = DecodeJwkInteger(jwk.Get("dp")); parameters.DQ = DecodeJwkInteger(jwk.Get("dq")); parameters.InverseQ = DecodeJwkInteger(jwk.Get("qi"));
                }
                if (privateKey)
                {
                    int width = parameters.Modulus.Length, half = (width + 1) / 2;
                    parameters.D = PadRsaPrivate(parameters.D, width);
                    parameters.P = PadRsaPrivate(parameters.P, half); parameters.Q = PadRsaPrivate(parameters.Q, half);
                    parameters.DP = PadRsaPrivate(parameters.DP, half); parameters.DQ = PadRsaPrivate(parameters.DQ, half); parameters.InverseQ = PadRsaPrivate(parameters.InverseQ, half);
                }
                rsa.ImportParameters(parameters);
            }
            else if (format is "pkcs8" or "spki")
            {
                byte[] bytes = CryptoBytes(input);
                try
                {
                    int read;
                    if (privateKey) rsa.ImportPkcs8PrivateKey(bytes, out read); else rsa.ImportSubjectPublicKeyInfo(bytes, out read);
                    if (read != bytes.Length) throw CryptoError("DataError", "Trailing RSA key data");
                }
                finally { if (privateKey) CryptographicOperations.ZeroMemory(bytes); }
            }
            else throw CryptoError("NotSupportedError", "Unsupported RSA key format");
            return RsaKey(rsa, hash, privateKey, extractable, usages);
        }
        catch (CryptographicException) { rsa.Dispose(); throw CryptoError("DataError", "Invalid RSA key data"); }
        catch { rsa.Dispose(); throw; }
        finally { ClearRsaPrivate(parameters); }
    }
    static object RsaExport(CryptoKeyValue key, string format)
    {
        if (format == "jwk")
        {
            var parameters = key.Rsa.ExportParameters(key.PrivateKey);
            try
            {
                var result = new JsDynamicObject();
                result.DefineOwn("kty", "RSA"); result.DefineOwn("n", EncodeRsaInteger(parameters.Modulus)); result.DefineOwn("e", EncodeRsaInteger(parameters.Exponent));
                if (key.PrivateKey)
                {
                    result.DefineOwn("d", EncodeRsaInteger(parameters.D)); result.DefineOwn("p", EncodeRsaInteger(parameters.P)); result.DefineOwn("q", EncodeRsaInteger(parameters.Q));
                    result.DefineOwn("dp", EncodeRsaInteger(parameters.DP)); result.DefineOwn("dq", EncodeRsaInteger(parameters.DQ)); result.DefineOwn("qi", EncodeRsaInteger(parameters.InverseQ));
                }
                result.DefineOwn("alg", RsaJwkAlgorithm(key.Hash)); result.DefineOwn("ext", BoxBoolean(key.Extractable));
                result.DefineOwn("key_ops", new JsArray(Array.ConvertAll(key.Usages, static usage => (object)usage))); return result;
            }
            finally { ClearRsaPrivate(parameters); }
        }
        byte[] bytes = format == "pkcs8" && key.PrivateKey ? key.Rsa.ExportPkcs8PrivateKey()
            : format == "spki" && !key.PrivateKey ? key.Rsa.ExportSubjectPublicKeyInfo()
            : throw CryptoError("NotSupportedError", "Key type does not support this RSA export format");
        return new BinaryBuffer(0) { Bytes = bytes };
    }
    static object RsaSign(object[] args, bool verify)
    {
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Name != RsaSigningName || key.Rsa == null || key.PrivateKey == verify || Array.IndexOf(key.Usages, verify ? "verify" : "sign") < 0)
            throw CryptoError("InvalidAccessError", "Key does not permit RSA signing operation");
        byte[] data = CryptoBytes(FunctionArgument(args, verify ? 3 : 2)); var hash = DerivationHash(key.Hash);
        try
        {
            if (verify) return BoxBoolean(key.Rsa.VerifyData(data, CryptoBytes(FunctionArgument(args, 2)), hash, RSASignaturePadding.Pkcs1));
            return new BinaryBuffer(0) { Bytes = key.Rsa.SignData(data, hash, RSASignaturePadding.Pkcs1) };
        }
        catch (CryptographicException)
        {
            if (verify) return BoxBoolean(false);
            throw CryptoError("OperationError", "RSA signing failed");
        }
    }
}
