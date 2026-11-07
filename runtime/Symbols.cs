using System;
using System.Collections.Generic;

internal sealed class JsSymbol(object description, string registryKey = null)
{
    internal string PropertyKey;
    internal readonly object Description = description;
    internal readonly string RegistryKey = registryKey;
    internal string Display => "Symbol(" + (Js.IsUndefined(Description) ? "" : (string)Description) + ")";
}

public static partial class Js
{
    static readonly JsSymbol IteratorSymbol = new("Symbol.iterator");
    static readonly JsSymbol AsyncDisposeSymbol = new("Symbol.asyncDispose");
    static readonly JsSymbol DisposeSymbol = new("Symbol.dispose");
    static readonly JsSymbol ConcatSpreadableSymbol = new("Symbol.isConcatSpreadable");
    static readonly JsSymbol HasInstanceSymbol = new("Symbol.hasInstance");
    static readonly JsSymbol ToPrimitiveSymbol = new("Symbol.toPrimitive");
    static readonly JsSymbol ToStringTagSymbol = new("Symbol.toStringTag");
    static readonly JsSymbol AsyncIteratorSymbol = new("Symbol.asyncIterator");
    static readonly Dictionary<string, JsSymbol> globalSymbols = new(StringComparer.Ordinal);
    static JsSymbol RequireSymbol(object receiver)
    {
        object value = receiver is JsBoxedPrimitive boxed ? boxed.Value : receiver;
        return value as JsSymbol ?? throw new InvalidOperationException("Symbol method requires a symbol");
    }
    sealed class SymbolFunction : NativeNamespaceFunction
    {
        static readonly HashSet<string> wellKnown = new(StringComparer.Ordinal) { "iterator", "asyncIterator", "toPrimitive", "toStringTag", "hasInstance", "species", "isConcatSpreadable", "match", "matchAll", "replace", "search", "split", "dispose", "asyncDispose", "unscopables" };
        internal override bool IsReadOnlyOwnProperty(string key) => wellKnown.Contains(key) || base.IsReadOnlyOwnProperty(key);
        internal override bool OwnPropertyConfigurable(string key) => !wellKnown.Contains(key) && base.OwnPropertyConfigurable(key);
        public override bool DeleteOwn(string key) => !wellKnown.Contains(key) && base.DeleteOwn(key);
        public override object Set(string key, object value) => wellKnown.Contains(key) ? throw new InvalidOperationException("Well-known symbols are read-only") : base.Set(key, value);
        internal SymbolFunction() : base("Symbol", static arguments =>
        {
            object description = FunctionArgument(arguments, 0);
            return new JsSymbol(IsUndefined(description) ? Undefined : StringValue(description));
        }, 0)
        {
            foreach (string name in wellKnown) Member(name, name == "iterator" ? IteratorSymbol : name == "asyncIterator" ? AsyncIteratorSymbol : name == "toStringTag" ? ToStringTagSymbol : name == "toPrimitive" ? ToPrimitiveSymbol : name == "hasInstance" ? HasInstanceSymbol : name == "isConcatSpreadable" ? ConcatSpreadableSymbol : name == "dispose" ? DisposeSymbol : name == "asyncDispose" ? AsyncDisposeSymbol : new JsSymbol("Symbol." + name));
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) => RequireSymbol(receiver).Display));
            prototype.DefineMethod("valueOf", new CollectionMethod("valueOf", 0, (receiver, _) => RequireSymbol(receiver)));
            prototype.DefineClassAccessor("description", new CollectionMethod("get description", 0, (receiver, _) => RequireSymbol(receiver).Description), 1);
            Member("for", new JsFunction((FunctionBody)(static arguments =>
            {
                string key = StringValue(FunctionArgument(arguments, 0));
                lock (globalSymbols)
                {
                    if (!globalSymbols.TryGetValue(key, out var symbol)) globalSymbols.Add(key, symbol = new JsSymbol(key, key));
                    return symbol;
                }
            }), null, "for", 1));
            Member("keyFor", new JsFunction((FunctionBody)(static arguments => FunctionArgument(arguments, 0) is JsSymbol symbol ? symbol.RegistryKey is string key ? key : Undefined : throw new InvalidOperationException("Symbol.keyFor requires a symbol")), null, "keyFor", 1));
        }
        public override object Construct(object[] arguments) => throw new InvalidOperationException("Symbol is not a constructor");
    }
}
