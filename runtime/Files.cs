using System;

public static partial class Js
{
    sealed class FileValue(byte[] bytes, string type, string name, double modified) : BlobValue(bytes, type)
    {
        internal readonly string Name = name;
        internal readonly double Modified = modified;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.File.Get("prototype");
    }
    static FileValue RequireFile(object value) => value as FileValue ?? throw new InvalidOperationException("File receiver required");
    static FileValue NewFile(object[] args)
    {
        if (args.Length < 2) throw new InvalidOperationException("File requires parts and a name");
        string name = QueryText(args[1]);
        object options = FunctionArgument(args, 2);
        var blob = NewBlob(new[] { args[0], options });
        object modified = options is TsObject bag ? bag.Get("lastModified") : Undefined;
        double value = IsUndefined(modified) ? DateNow() : Number(modified);
        double timestamp = double.IsNaN(value) ? 0 : value;
        return new FileValue(blob.Bytes, blob.Type, name, timestamp);
    }
    sealed class FileConstructor : NativeNamespaceFunction
    {
        internal FileConstructor() : base("File", static _ => throw new InvalidOperationException("File requires new"), 2)
        {
            var prototype = new JsClassInstance { ParentPrototype = (TsObject)RuntimeNamespaces.Blob.Get("prototype") }; UseObjectPrototype(prototype);
            SetPrototype(RuntimeNamespaces.Blob);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "File");
            prototype.DefineClassAccessor("name", new CollectionMethod("get name", 0, (receiver, _) => RequireFile(receiver).Name), 1);
            prototype.DefineClassAccessor("lastModified", new CollectionMethod("get lastModified", 0, (receiver, _) => (double)RequireFile(receiver).Modified), 1);
        }
        public override object Construct(object[] args) => NewFile(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
