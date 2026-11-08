using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;

public static partial class Js
{
    static void CheckFetchIntegrity(string metadata, byte[] bytes)
    {
        if (metadata.Length == 0) return;
        if (bytes == null) throw new HttpRequestException("Fetch integrity requires a response body");
        int strongest = 0;
        var candidates = new List<string>();
        foreach (string item in metadata.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string expression = item.Split('?')[0];
            int separator = expression.IndexOf('-');
            string algorithm = separator < 0 ? expression : expression[..separator];
            int strength = algorithm switch { "sha256" => 256, "sha384" => 384, "sha512" => 512, _ => 0 };
            if (strength == 0 || strength < strongest) continue;
            if (strength > strongest) { candidates.Clear(); strongest = strength; }
            candidates.Add(separator < 0 ? "" : expression[(separator + 1)..]);
        }
        if (strongest == 0) return;
        byte[] digest = strongest switch { 256 => SHA256.HashData(bytes), 384 => SHA384.HashData(bytes), _ => SHA512.HashData(bytes) };
        string expected = FetchIntegrityDigestText(Convert.ToBase64String(digest));
        foreach (string candidate in candidates)
            if (FetchIntegrityDigestText(candidate) == expected) return;
        throw new HttpRequestException("Fetch integrity check failed");
    }
    static string FetchIntegrityDigestText(string value)
    {
        int length = value.Length;
        for (int padding = 0; padding < 2 && length > 0 && value[length - 1] == '='; padding++) length--;
        return value[..length].Replace('-', '+').Replace('_', '/');
    }
}
