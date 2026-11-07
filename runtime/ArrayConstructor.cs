using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    internal static TsObject ArrayPrototype => (TsObject)RuntimeNamespaces.Array.Get("prototype");
    static JsArray CreateArray(object[] arguments)
    {
        var result = new JsArray(0);
        if (arguments.Length == 1 && arguments[0] is double length) result.SetLength(length);
        else foreach (object argument in arguments) result.Append(argument);
        return result;
    }
    sealed class ArrayPrototypeObject() : JsArray(0)
    {
        readonly HashSet<string> hidden = new();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : DefaultClassObjectPrototype;
        internal void Member(string key, object value) { DefineOwn(key, value); hidden.Add(key); }
        public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden.Remove(key); return removed; }
        internal override bool ForInEnumerable(string key) => !hidden.Contains(key) && base.ForInEnumerable(key);
        internal override object ForInOwnKeys() => ForInNames(base.OwnKeys(), "length");
        internal override object OwnPropertyNames() => MergeOwnNames(this, base.OwnKeys(), "length");
        public override double OwnKeyCount() => ((JsArray)OwnKeys()).Length();
        public override object OwnKeys()
        {
            var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
            for (uint i = 0; i < keys.Length(); i++) { string key = (string)keys.ElementAt(i); if (!hidden.Contains(key)) result.Append(key); }
            return result;
        }
    }
    sealed class ArrayConstructorFunction : NativeNamespaceFunction
    {
        internal ArrayConstructorFunction() : base("Array", static args => CreateArray(args), 1)
        {
            var prototype = new ArrayPrototypeObject();
            UseObjectPrototype(prototype); prototype.Member("constructor", this); prototype.Member("toString", new ArrayStringFunction());
            foreach (string name in new[] { "at", "push", "pop", "slice", "reverse", "indexOf", "includes", "join", "map", "filter", "forEach", "every", "some", "reduce", "reduceRight", "find", "findIndex", "findLast", "findLastIndex", "flat", "flatMap", "lastIndexOf", "toReversed", "toSorted", "sort", "toSpliced", "with", "fill", "copyWithin" })
                prototype.Member(name, ArrayMethodValue(name));
            Member("isArray", new JsFunction((FunctionBody)(static args => BoxBoolean(FunctionArgument(args, 0) is JsArray)), null, "isArray", 1));
            Member("of", new ArrayOfFunction()); Member("from", new ArrayFromFunction());
        }
        public override object Construct(object[] arguments) => CreateArray(arguments);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var result = CreateArray(arguments);
            result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;
        }
    }
    sealed class ArrayOfFunction() : JsFunction((FunctionBody)(static args => ArrayOf(Undefined, args)), null, "of", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ArrayOf(receiver, arguments);
    }
    static object ArrayOf(object receiver, object[] arguments)
    {
        var result = receiver is JsFunction constructor && constructor.CanInitializeDerived
            ? (TsObject)constructor.Construct(new object[] { (double)arguments.Length }) : new JsArray(0);
        for (int i = 0; i < arguments.Length; i++)
        {
            if (result is JsArray array) array.SetAt(i, arguments[i]);
            else
            {
                string key = i.ToString(CultureInfo.InvariantCulture);
                if (result.IsModuleNamespace || result.IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Cannot define array item");
                result.DefineOwn(key, arguments[i]);
            }
        }
        result.Set("length", (double)arguments.Length);
        return result;
    }
}
