using System;
using System.Runtime.CompilerServices;
using System.Threading;

public static partial class Js
{
    // Existing object layouts use string slots. Opaque keys preserve symbol
    // identity without changing those layouts; the weak table keeps a symbol
    // alive while an object still holds its key, but does not retain dead keys.
    static readonly ConditionalWeakTable<string, JsSymbol> symbolKeys = new();
    static readonly string symbolKeyPrefix = "\0symbol:" + Guid.NewGuid().ToString("N") + ":";
    static long nextSymbolKey;
    static bool haveSymbolKeys;

    static string SymbolPropertyKey(JsSymbol symbol)
    {
        if (symbol.PropertyKey != null) return symbol.PropertyKey;
        string key = symbolKeyPrefix + Interlocked.Increment(ref nextSymbolKey);
        symbolKeys.Add(key, symbol);
        haveSymbolKeys = true;
        return symbol.PropertyKey = key;
    }

    internal static bool IsSymbolKey(string key) => haveSymbolKeys && key.Length != 0 && key[0] == '\0' && symbolKeys.TryGetValue(key, out _);
    internal static string PropertyFunctionName(string key)
    {
        if (!haveSymbolKeys || key.Length == 0 || key[0] != '\0' || !symbolKeys.TryGetValue(key, out var symbol)) return key;
        return IsUndefined(symbol.Description) ? "" : "[" + (string)symbol.Description + "]";
    }

    internal static double StringPropertyKeyCount(object names)
    {
        var keys = (JsArray)names;
        double count = keys.Length();
        if (haveSymbolKeys)
            for (uint i = 0; i < keys.Length(); i++)
                if (IsSymbolKey((string)keys.ElementAt(i))) count--;
        return count;
    }

    internal static JsArray StringPropertyKeys(object names)
    {
        var keys = (JsArray)names;
        if (!haveSymbolKeys) return keys;
        JsArray result = null;
        for (uint i = 0; i < keys.Length(); i++) {
            string key = (string)keys.ElementAt(i);
            if (IsSymbolKey(key)) {
                if (result == null) {
                    result = new JsArray(0);
                    for (uint j = 0; j < i; j++) result.Append(keys.ElementAt(j));
                }
            } else result?.Append(key);
        }
        return result ?? keys;
    }

    static object SymbolPropertyKeys(object names)
    {
        var keys = (JsArray)names;
        var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++)
            if (symbolKeys.TryGetValue((string)keys.ElementAt(i), out var symbol)) result.Append(symbol);
        return result;
    }

    static object ReflectPropertyKeys(object names)
    {
        var keys = (JsArray)names;
        if (!haveSymbolKeys) return keys;
        var result = new JsArray(0);
        for (uint i = 0; i < keys.Length(); i++) {
            string key = (string)keys.ElementAt(i);
            result.Append(symbolKeys.TryGetValue(key, out var symbol) ? symbol : key);
        }
        return result;
    }
}
