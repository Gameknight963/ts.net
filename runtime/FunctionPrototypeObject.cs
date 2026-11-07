using System;
using System.Collections.Generic;

public static partial class Js
{
    static class FunctionPrototypeState
    {
        internal static readonly TsObject Value = new FunctionPrototypeValue();
    }
    internal static TsObject SharedFunctionPrototype => FunctionPrototypeState.Value;
    sealed class FunctionPrototypeValue : JsFunction
    {
        readonly HashSet<string> hidden = new() {"call", "apply", "bind", "toString"};
        internal FunctionPrototypeValue() : base((FunctionBody)(static _ => Undefined), null, "", 0)
        {
            foreach (string key in hidden) DefineOwn(key, key == "toString" ? new FunctionStringFunction() : FunctionMethod(key));
        }
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
            bool removed = base.DeleteOwn(key);
            if (removed) hidden.Remove(key);
            return removed;
        }
    }
}
