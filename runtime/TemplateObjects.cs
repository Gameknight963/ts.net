using System;
using System.Collections.Generic;

internal sealed class JsTemplateArray(object[] items, JsTemplateArray raw = null) : JsArray(items)
{
    internal override bool IsExtensible => false;
    static InvalidOperationException Frozen() => new("Template strings arrays are frozen");
    public override bool HasOwn(string key) => key == "raw" && raw != null || base.HasOwn(key);
    public override object Get(string key) => key == "raw" && raw != null ? raw : base.Get(key);
    public override object Set(string key, object value) => throw Frozen();
    public override object SetOwnValue(string key, object value) => throw Frozen();
    protected override void Write(uint index, object value) => throw Frozen();
    protected override void Resize(uint length) => throw Frozen();
    protected override void DeleteElement(uint index) => throw Frozen();
    public override bool DeleteOwn(string key) => !HasOwn(key);
    internal override bool IsReadOnlyOwnProperty(string key) => HasOwn(key);
    internal override bool ForInEnumerable(string key) => key != "raw" && base.ForInEnumerable(key);
    internal override object OwnPropertyNames() => raw == null ? base.OwnPropertyNames() : Js.MergeOwnNames(this, base.OwnKeys(), "length", "raw");
    internal override bool TrySetPrototype(TsObject value) => ReferenceEquals(value, PropertyPrototype);
    internal override void SetPrototype(TsObject value) { if (!ReferenceEquals(value, PropertyPrototype)) throw Frozen(); }
    public override JsArray Reverse() { if (Length() > 1) throw Frozen(); return this; }
}

public static partial class Js
{
    static readonly Dictionary<string, JsTemplateArray> templateObjects = new();
    static class TemplateObjectFunction
    {
        internal static readonly JsFunction Value = new((FunctionBody)(static args =>
        {
            string key = (string)FunctionArgument(args, 0);
            lock (templateObjects)
            {
                if (templateObjects.TryGetValue(key, out var cached)) return cached;
                object[] Copy(object value)
                {
                    var source = (JsArray)value; var result = new object[(int)source.Length()];
                    for (int index = 0; index < result.Length; index++) result[index] = source.ElementAt((uint)index);
                    return result;
                }
                var result = new JsTemplateArray(Copy(FunctionArgument(args, 1)), new JsTemplateArray(Copy(FunctionArgument(args, 2))));
                templateObjects.Add(key, result); return result;
            }
        }), null, "templateObject", 3);
    }
}
