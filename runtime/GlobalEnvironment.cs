using System;
using System.Collections.Generic;

public static partial class Js
{
    // Global properties are explicit runtime state; compiler locals remain locals.
    sealed class GlobalObject : JsClassInstance
    {
        readonly HashSet<string> native = new(StringComparer.Ordinal);
        internal GlobalObject()
        {
            ParentPrototype = DefaultClassObjectPrototype;
            void Binding(string name, object value) { DefineMethod(name, value); native.Add(name); }
            foreach (string name in new[] {
                "Object", "Number", "String", "Boolean", "Array", "BigInt", "Symbol", "Date", "RegExp", "Math", "JSON", "Reflect", "Intl",
                "console", "performance", "Promise", "queueMicrotask", "Map", "Set", "WeakMap", "WeakSet", "WeakRef", "Iterator",
                "DisposableStack", "AsyncDisposableStack", "ArrayBuffer", "SharedArrayBuffer", "Atomics", "DataView", "TextEncoder", "TextDecoder", "Blob", "File", "URL", "URLSearchParams", "Headers", "Response", "Request",
                "crypto", "Crypto", "SubtleCrypto", "CryptoKey", "DOMException", "Event", "CustomEvent", "EventTarget", "AbortController", "AbortSignal", "setTimeout", "setInterval", "clearTimeout", "clearInterval", "structuredClone",
                "parseInt", "parseFloat", "isNaN", "isFinite", "atob", "btoa", "encodeURI", "encodeURIComponent", "decodeURI", "decodeURIComponent", "escape", "unescape"
            }) Binding(name, BuiltinNamespace(name));
            foreach (string name in TypedArrayNames) Binding(name, GetTypedArrayConstructor(name));
            foreach (string name in new[] { "Error", "TypeError", "RangeError", "ReferenceError", "SyntaxError", "URIError", "EvalError", "AggregateError", "SuppressedError" }) Binding(name, GetErrorConstructor(name));
            Binding("undefined", Undefined); Binding("NaN", double.NaN); Binding("Infinity", double.PositiveInfinity); Binding("globalThis", this);
        }
        internal override bool IsReadOnlyOwnProperty(string key) => native.Contains(key);
        public override object SetOwnValue(string key, object value)
        {
            if (native.Contains(key)) throw new InvalidOperationException("Native global bindings are read-only");
            return base.SetOwnValue(key, value);
        }
        public override object SetWithReceiver(string key, object value, object receiver)
        {
            if (native.Contains(key)) throw new InvalidOperationException("Native global bindings are read-only");
            return base.SetWithReceiver(key, value, receiver);
        }
    }
    static class GlobalEnvironment
    {
        internal static readonly TsObject Value = new GlobalObject();
    }
}
