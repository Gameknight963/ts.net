using System;
using System.Collections.Generic;

public static partial class Js
{
    static class FunctionPrototypeState
    {
        internal static readonly TsObject Value = new FunctionPrototypeValue();
    }
    internal static TsObject SharedFunctionPrototype => FunctionPrototypeState.Value;
    static class OrdinaryInstanceCheck
    {
        internal static readonly JsFunction Method = new CollectionMethod("[Symbol.hasInstance]", 1,
            (receiver, args) => BoxBoolean(receiver is JsFunction function && function.HasInstance(FunctionArgument(args, 0))));
    }
    sealed class FunctionPrototypeValue : JsFunction
    {
        readonly string instanceKey = PropertyKey(HasInstanceSymbol);
        readonly HashSet<string> hidden = new() {"call", "apply", "bind", "toString"};
        internal FunctionPrototypeValue() : base((FunctionBody)(static _ => Undefined), null, "", 0)
        {
            hidden.Add(instanceKey);
            foreach (string key in hidden) DefineOwn(key, key == instanceKey ? OrdinaryInstanceCheck.Method : key == "toString" ? new FunctionStringFunction() : FunctionMethod(key));
        }
        internal override bool IsReadOnlyOwnProperty(string key) => key == instanceKey || base.IsReadOnlyOwnProperty(key);
        internal override bool OwnPropertyConfigurable(string key) => key != instanceKey && base.OwnPropertyConfigurable(key);
        public override object Set(string key, object value) => key == instanceKey ? throw new InvalidOperationException("Symbol.hasInstance is read-only") : base.Set(key, value);
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : DefaultClassObjectPrototype;
        internal override bool ForInEnumerable(string key) => !hidden.Contains(key) && base.ForInEnumerable(key);
        internal override object ForInOwnKeys() => base.ForInOwnKeys();
        public override object OwnKeys()
        {
            var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
            for (uint index = 0; index < keys.Length(); index++)
            {
                string key = (string)keys.ElementAt(index);
                if (!hidden.Contains(key)) result.Append(key);
            }
            return result;
        }
        public override bool DeleteOwn(string key)
        {
            if (key == instanceKey) return false;
            bool removed = base.DeleteOwn(key);
            if (removed) hidden.Remove(key);
            return removed;
        }
    }
}
