using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class FormDataValue : TsObject
    {
        internal readonly List<KeyValuePair<string,object>> Fields = new();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)FormDataConstructors.Value.Get("prototype");
        internal void SetField(string name, object value)
        {
            bool found = false; int write = 0;
            for (int read = 0; read < Fields.Count; read++) {
                var pair = Fields[read];
                if (pair.Key == name) {
                    if (found) continue;
                    found = true; pair = new(name,value);
                }
                Fields[write++] = pair;
            }
            if (write < Fields.Count) Fields.RemoveRange(write,Fields.Count-write);
            if (!found) Fields.Add(new(name,value));
        }
    }
    static FormDataValue RequireFormData(object receiver) => receiver as FormDataValue ?? throw new InvalidOperationException("FormData receiver required");
    static object FormDataField(object[] args)
    {
        if (args.Length < 2) throw new InvalidOperationException("FormData requires a name and value");
        object value = args[1], filename = FunctionArgument(args,2);
        if (value is not BlobValue blob) {
            if (args.Length > 2) throw new InvalidOperationException("A filename requires a Blob value");
            return QueryText(value);
        }
        if (IsUndefined(filename) && blob is FileValue existing) return existing;
        string name = IsUndefined(filename) ? "blob" : QueryText(filename);
        return new FileValue(blob.Bytes,blob.Type,name,blob is FileValue file ? file.Modified : DateNow());
    }
    sealed class FormDataIterator(FormDataValue source, int mode) : TsObject
    {
        int position;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : FormDataIteratorMethods.Prototype;
        internal object Next()
        {
            if (position >= source.Fields.Count) return IteratorResult(Undefined,true);
            var pair = source.Fields[position++];
            return IteratorResult(mode == 1 ? pair.Key : mode == 2 ? pair.Value : new JsArray(new object[] {pair.Key,pair.Value}),false);
        }
    }
    static class FormDataIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = NativeIteratorPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"FormData Iterator");
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => receiver is FormDataIterator iterator ? iterator.Next() : throw new InvalidOperationException("FormData iterator receiver required")));
            return result;
        }
    }
    static class FormDataConstructors {internal static readonly JsFunction Value = new FormDataConstructor();}
    sealed class FormDataConstructor : NativeNamespaceFunction
    {
        static FormDataValue Create(object[] args)
        {
            if (!IsUndefined(FunctionArgument(args,0))) throw new InvalidOperationException("HTML forms are unavailable on this platform");
            return new FormDataValue();
        }
        internal FormDataConstructor() : base("FormData",static _ => throw new InvalidOperationException("FormData constructor requires new"),0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"FormData");
            foreach (bool replace in new[] {false,true}) {
                string name = replace ? "set" : "append";
                prototype.DefineOwn(name,new CollectionMethod(name,2,(receiver,args) => {
                    var target = RequireFormData(receiver);
                    if (args.Length < 2) throw new InvalidOperationException("FormData requires a name and value");
                    string key = QueryText(args[0]); object value = FormDataField(args);
                    if (replace) target.SetField(key,value); else target.Fields.Add(new(key,value));
                    return Undefined;
                }));
            }
            foreach (string name in new[] {"get","getAll","has","delete"}) {
                prototype.DefineOwn(name,new CollectionMethod(name,1,(receiver,args) => {
                    var target = RequireFormData(receiver); string key = QueryArgument(args,0);
                    if (name == "delete") {target.Fields.RemoveAll(pair => pair.Key == key);return Undefined;}
                    if (name == "getAll") {var result = new JsArray(0);foreach (var pair in target.Fields) if (pair.Key == key) result.Append(pair.Value);return result;}
                    foreach (var pair in target.Fields) if (pair.Key == key) return name == "has" ? BoxedTrue : pair.Value;
                    return name == "has" ? BoxedFalse : null;
                }));
            }
            prototype.DefineOwn("forEach",new CollectionMethod("forEach",1,(receiver,args) => {
                var target = RequireFormData(receiver);
                if (FunctionArgument(args,0) is not JsFunction callback) throw new InvalidOperationException("FormData callback must be callable");
                for (int index = 0; index < target.Fields.Count; index++) {
                    var pair = target.Fields[index]; callback.InvokeWithReceiver(FunctionArgument(args,1),new object[] {pair.Value,pair.Key,target});
                }
                return Undefined;
            }));
            foreach (var item in new[] {("entries",0),("keys",1),("values",2)})
                prototype.DefineOwn(item.Item1,new CollectionMethod(item.Item1,0,(receiver,_) => new FormDataIterator(RequireFormData(receiver),item.Item2)));
            prototype.DefineMethod(PropertyKey(IteratorSymbol),prototype.Get("entries"));
        }
        public override object Construct(object[] args) => Create(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = Create(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
}
