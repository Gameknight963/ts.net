using System;
using System.Runtime.CompilerServices;

public sealed class JsWeakCollection : JsDescriptorObject
{
    sealed class Cell(object value) { internal object Value = value; }
    readonly ConditionalWeakTable<object, Cell> entries = new();
    internal readonly bool IsSet;
    internal JsWeakCollection(bool set) { IsSet = set; }
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.WeakCollectionPrototype(IsSet);
    internal object Read(object key) => Js.CanBeWeakKey(key) && entries.TryGetValue(key, out var cell) ? cell.Value : Js.Undefined;
    internal bool Contains(object key) => Js.CanBeWeakKey(key) && entries.TryGetValue(key, out _);
    internal bool Remove(object key) => Js.CanBeWeakKey(key) && entries.Remove(key);
    internal void Write(object key, object value)
    {
        if (!Js.CanBeWeakKey(key)) throw new InvalidOperationException("Weak collection key must be an object or non-registered symbol");
        entries.GetValue(key, static _ => new Cell(Js.Undefined)).Value = IsSet ? Js.Undefined : value;
    }
}

public static partial class Js
{
    internal static bool CanBeWeakKey(object key) => key is TsObject or JsSymbol { RegistryKey: null };
    internal static TsObject WeakCollectionPrototype(bool set) => (TsObject)(set ? RuntimeNamespaces.WeakSet : RuntimeNamespaces.WeakMap).Get("prototype");
    sealed class WeakCollectionConstructor : NativeNamespaceFunction
    {
        readonly bool set;
        internal WeakCollectionConstructor(bool set) : base(set ? "WeakSet" : "WeakMap", static _ => throw new InvalidOperationException("Weak collection constructor requires new"), 0)
        {
            this.set = set;
            var prototype = new JsCreatedObject();
            prototype.SetPrototype(DefaultClassObjectPrototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), set ? "WeakSet" : "WeakMap", false);
            CollectionMethod Method(string name, int length, Func<JsWeakCollection, object[], object> action) => new(name, length,
                (receiver, arguments) => receiver is JsWeakCollection collection && collection.IsSet == set
                    ? action(collection, arguments) : throw new InvalidOperationException("Incompatible weak collection receiver"));
            prototype.DefineMethod("has", Method("has", 1, (collection, args) => BoxBoolean(collection.Contains(FunctionArgument(args, 0)))));
            prototype.DefineMethod("delete", Method("delete", 1, (collection, args) => BoxBoolean(collection.Remove(FunctionArgument(args, 0)))));
            prototype.DefineMethod(set ? "add" : "set", Method(set ? "add" : "set", set ? 1 : 2, (collection, args) =>
            {
                collection.Write(FunctionArgument(args, 0), FunctionArgument(args, 1)); return collection;
            }));
            if (!set) prototype.DefineMethod("get", Method("get", 1, (collection, args) => collection.Read(FunctionArgument(args, 0))));
            if (!set) foreach (bool computed in new[] {false,true}) {
                string name = computed ? "getOrInsertComputed" : "getOrInsert";
                prototype.DefineMethod(name,Method(name,2,(collection,args) => {
                    object key = FunctionArgument(args,0);
                    if (!CanBeWeakKey(key)) throw new InvalidOperationException("WeakMap key must be an object or non-registered symbol");
                    JsFunction callback = computed ? FunctionArgument(args,1) as JsFunction ?? throw new InvalidOperationException("WeakMap callback must be callable") : null;
                    if (collection.Contains(key)) return collection.Read(key);
                    object value = computed ? callback.InvokeWithReceiver(Undefined,new[] {key}) : FunctionArgument(args,1);
                    collection.Write(key,value); return value;
                }));
            }
            UseObjectPrototype(prototype);
        }
        object Create(object[] arguments, TsObject prototype)
        {
            var result = new JsWeakCollection(set); if (prototype != null) result.SetPrototype(prototype);
            object input = FunctionArgument(arguments, 0);
            if (IsNullish(input)) return result;
            object adder = result.Get(set ? "add" : "set");
            if (adder is not JsFunction) throw new InvalidOperationException("Weak collection adder is not callable");
            object source = Iteration.Start(input);
            int position = 0; bool closeable = false;
            try
            {
                while (true)
                {
                    closeable = false;
                    if (position >= Iteration.Length(source)) break;
                    object value = Iteration.Value(source, position);
                    position += Iteration.Step(source, position); closeable = true;
                    if (set) CallMethod(adder, result, new[] { value });
                    else
                    {
                        if (value is not TsObject) throw new InvalidOperationException("WeakMap entry is not an object");
                        object key = Js.Get(value, "0"), item = Js.Get(value, "1");
                        CallMethod(adder, result, new[] { key, item });
                    }
                }
            }
            catch { if (closeable) CloseIterationAt(source, position, true); throw; }
            return result;
        }
        public override object Construct(object[] arguments) => Create(arguments, null);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) => Create(arguments, ((TsObject)instance).PropertyPrototype);
    }
}

