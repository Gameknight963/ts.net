using System;
using System.Collections.Generic;
using System.Security.Cryptography;

public static partial class Js
{
    static string EcCurveName(object algorithm)
    {
        if (algorithm is not TsObject options) throw new InvalidOperationException("EC algorithm requires namedCurve");
        object value = options.Get("namedCurve");
        if (IsUndefined(value)) throw new InvalidOperationException("EC algorithm requires namedCurve");
        string name = StringValue(value);
        _ = EcCurve(name); return name;
    }
    static ECCurve EcCurve(string name) => name switch
    {
        "P-256" => ECCurve.NamedCurves.nistP256,
        "P-384" => ECCurve.NamedCurves.nistP384,
        "P-521" => ECCurve.NamedCurves.nistP521,
        _ => throw CryptoError("NotSupportedError", "Unsupported elliptic curve")
    };
    static int EcWidth(string curve) => curve == "P-521" ? 66 : curve == "P-384" ? 48 : 32;
    static string[] EcUsages(object value, bool? privateKey, string name)
    {
        var usages = new List<string>(); object iterator = Iteration.Start(value); int position = 0;
        try
        {
            while (position < Iteration.Length(iterator))
            {
                string usage = StringValue(Iteration.Value(iterator, position));
                bool valid = name == "ECDSA" ? usage is "sign" or "verify" && (privateKey != true || usage == "sign") && (privateKey != false || usage == "verify")
                    : usage is "deriveBits" or "deriveKey" && privateKey != false;
                if (!valid) throw CryptoError("SyntaxError", "Invalid EC key usage");
                if (!usages.Contains(usage)) usages.Add(usage);
                position += Iteration.Step(iterator, position);
            }
        }
        catch { CloseIterationAt(iterator, position, true); throw; }
        if (privateKey == true && usages.Count == 0) throw CryptoError("SyntaxError", "Private key requires a usage");
        return usages.ToArray();
    }
    static CryptoKeyValue EcKey(ECDsa ec, string curve, bool privateKey, bool extractable, string[] usages, string name) =>
        new(Array.Empty<byte>(), null, extractable, usages, name, ec, curve, privateKey);
    static object EcGenerate(object[] args, string name = "ECDSA")
    {
        string curve = EcCurveName(FunctionArgument(args, 0));
        string[] usages = EcUsages(FunctionArgument(args, 2), null, name);
        if (name == "ECDSA" ? Array.IndexOf(usages, "sign") < 0 : usages.Length == 0) throw CryptoError("SyntaxError", "Private key requires a usage");
        ECDsa privateKey = null, publicKey = null;
        try
        {
            privateKey = ECDsa.Create(EcCurve(curve)); publicKey = ECDsa.Create(privateKey.ExportParameters(false));
            var result = new JsDynamicObject();
            result.DefineOwn("privateKey", EcKey(privateKey, curve, true, Truthy(FunctionArgument(args, 1)), name == "ECDSA" ? new[] { "sign" } : usages, name));
            result.DefineOwn("publicKey", EcKey(publicKey, curve, false, true, name != "ECDSA" || Array.IndexOf(usages, "verify") < 0 ? Array.Empty<string>() : new[] { "verify" }, name));
            return result;
        }
        catch { privateKey?.Dispose(); publicKey?.Dispose(); throw; }
    }
    static byte[] EcDecode(object value, int length)
    {
        if (IsUndefined(value)) throw CryptoError("DataError", "Missing EC coordinate");
        string text = StringValue(value);
        foreach (char ch in text) if (!(ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
            throw CryptoError("DataError", "Invalid EC coordinate encoding");
        try
        {
            byte[] bytes = Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/').PadRight((text.Length + 3) / 4 * 4, '='));
            if (bytes.Length != length) throw CryptoError("DataError", "Invalid EC coordinate length");
            return bytes;
        }
        catch (FormatException) { throw CryptoError("DataError", "Invalid EC coordinate encoding"); }
    }
    static void EcJwkPermissions(TsObject jwk, string curve, bool extractable, string[] usages, string name)
    {
        object alg = jwk.Get("alg"), use = jwk.Get("use"), ext = jwk.Get("ext");
        string expected = curve == "P-521" ? "ES512" : curve == "P-384" ? "ES384" : "ES256";
        if (name == "ECDSA" && !IsUndefined(alg) && StringValue(alg) != expected) throw CryptoError("DataError", "JWK algorithm does not match curve");
        if (!IsUndefined(use) && usages.Length != 0 && StringValue(use) != (name == "ECDSA" ? "sig" : "enc")) throw CryptoError("DataError", "JWK does not permit signatures");
        if (!IsUndefined(ext) && !Truthy(ext) && extractable) throw CryptoError("DataError", "JWK does not permit extraction");
        object operations = jwk.Get("key_ops");
        if (!IsUndefined(operations))
        {
            var allowed = new HashSet<string>(); object iterator = Iteration.Start(operations); int position = 0;
            while (position < Iteration.Length(iterator))
            {
                if (!allowed.Add(StringValue(Iteration.Value(iterator, position)))) throw CryptoError("DataError", "Duplicate JWK key operation");
                position += Iteration.Step(iterator, position);
            }
            foreach (string usage in usages) if (!allowed.Contains(usage)) throw CryptoError("DataError", "JWK does not permit requested usage");
        }
    }
    static object EcImport(object[] args, string name = "ECDSA")
    {
        string format = StringValue(FunctionArgument(args, 0)), curve = EcCurveName(FunctionArgument(args, 2));
        object input = FunctionArgument(args, 1); bool extractable = Truthy(FunctionArgument(args, 3));
        bool privateKey = format == "pkcs8" || format == "jwk" && input is TsObject j && !IsUndefined(j.Get("d"));
        string[] usages = EcUsages(FunctionArgument(args, 4), privateKey, name);
        ECDsa ec = ECDsa.Create(); byte[] secret = null;
        try
        {
            int width = EcWidth(curve);
            if (format == "jwk")
            {
                if (input is not TsObject jwk) throw new InvalidOperationException("JWK must be an object");
                if (StringValue(jwk.Get("kty")) != "EC" || StringValue(jwk.Get("crv")) != curve) throw CryptoError("DataError", "JWK curve does not match");
                EcJwkPermissions(jwk, curve, extractable, usages, name);
                secret = privateKey ? EcDecode(jwk.Get("d"), width) : null;
                ec.ImportParameters(new ECParameters { Curve = EcCurve(curve), Q = new ECPoint { X = EcDecode(jwk.Get("x"), width), Y = EcDecode(jwk.Get("y"), width) }, D = secret });
            }
            else
            {
                byte[] bytes = CryptoBytes(input); int read;
                if (format == "raw")
                {
                    if (bytes.Length != 1 + 2 * width || bytes[0] != 4) throw CryptoError("DataError", "Invalid EC public point");
                    ec.ImportParameters(new ECParameters { Curve = EcCurve(curve), Q = new ECPoint { X = bytes.AsSpan(1, width).ToArray(), Y = bytes.AsSpan(1 + width, width).ToArray() } });
                }
                else if (format == "spki") { ec.ImportSubjectPublicKeyInfo(bytes, out read); if (read != bytes.Length) throw CryptoError("DataError", "Trailing EC key data"); }
                else if (format == "pkcs8") { try { ec.ImportPkcs8PrivateKey(bytes, out read); if (read != bytes.Length) throw CryptoError("DataError", "Trailing EC key data"); } finally { CryptographicOperations.ZeroMemory(bytes); } }
                else throw CryptoError("NotSupportedError", "Unsupported ECDSA key format");
            }
            if (ec.ExportParameters(false).Curve.Oid.Value != EcCurve(curve).Oid.Value) throw CryptoError("DataError", "EC curve does not match");
            return EcKey(ec, curve, privateKey, extractable, usages, name);
        }
        catch (CryptographicException) { ec.Dispose(); throw CryptoError("DataError", "Invalid EC key data"); }
        catch { ec.Dispose(); throw; }
        finally { if (secret != null) CryptographicOperations.ZeroMemory(secret); }
    }
    static string EcEncode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    static object EcExport(CryptoKeyValue key, string format)
    {
        byte[] bytes;
        if (format == "jwk" || format == "raw" && !key.PrivateKey)
        {
            var parameters = key.Ec.ExportParameters(format == "jwk" && key.PrivateKey);
            try
            {
                if (format == "raw") { bytes = new byte[1 + parameters.Q.X.Length + parameters.Q.Y.Length]; bytes[0] = 4; parameters.Q.X.CopyTo(bytes, 1); parameters.Q.Y.CopyTo(bytes, 1 + parameters.Q.X.Length); }
                else
                {
                    var jwk = new JsDynamicObject(); jwk.DefineOwn("kty", "EC"); jwk.DefineOwn("crv", key.Curve);
                    jwk.DefineOwn("x", EcEncode(parameters.Q.X)); jwk.DefineOwn("y", EcEncode(parameters.Q.Y));
                    if (parameters.D != null) jwk.DefineOwn("d", EcEncode(parameters.D));
                    jwk.DefineOwn("ext", BoxBoolean(key.Extractable)); jwk.DefineOwn("key_ops", new JsArray(Array.ConvertAll(key.Usages, static usage => (object)usage))); return jwk;
                }
            }
            finally { if (parameters.D != null) CryptographicOperations.ZeroMemory(parameters.D); }
        }
        else if (format == "spki" && !key.PrivateKey) bytes = key.Ec.ExportSubjectPublicKeyInfo();
        else if (format == "pkcs8" && key.PrivateKey) bytes = key.Ec.ExportPkcs8PrivateKey();
        else throw CryptoError("NotSupportedError", "Key type does not support this export format");
        return new BinaryBuffer(0) { Bytes = bytes };
    }
    static object EcSign(object[] args, bool verify)
    {
        var key = RequireCryptoKey(FunctionArgument(args, 1));
        if (key.Ec == null || key.PrivateKey == verify || Array.IndexOf(key.Usages, verify ? "verify" : "sign") < 0)
            throw CryptoError("InvalidAccessError", "Key does not permit this operation");
        string hash = HmacHash(FunctionArgument(args, 0));
        var algorithm = new HashAlgorithmName(hash.Replace("-", ""));
        byte[] data = CryptoBytes(FunctionArgument(args, verify ? 3 : 2));
        if (verify)
        {
            byte[] signature = CryptoBytes(FunctionArgument(args, 2));
            if (signature.Length != 2 * EcWidth(key.Curve)) return BoxBoolean(false);
            return BoxBoolean(key.Ec.VerifyData(data, signature, algorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }
        return new BinaryBuffer(0) { Bytes = key.Ec.SignData(data, algorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation) };
    }
}
