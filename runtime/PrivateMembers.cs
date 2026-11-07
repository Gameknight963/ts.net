using System;
using System.Runtime.CompilerServices;

internal enum JsPrivateKind { Field, Method, Getter, Setter, AutoAccessor }

internal sealed class JsPrivateMember(string name, JsPrivateKind kind)
{
    sealed class Cell(object value, bool initialized)
    {
        internal object Value = value;
        internal bool Initialized = initialized;
    }
    readonly ConditionalWeakTable<TsObject, Cell> values = new();
    JsFunction method, getter, setter;
    static Exception Error(string message) => Js.MakeThrown(new JsCaughtError("TypeError", message));
    static TsObject Target(object receiver) => Js.ResolveThis(receiver) as TsObject ?? throw Error("Private member receiver must be an object");
    Cell Storage(object receiver) => values.TryGetValue(Target(receiver), out var cell) ? cell : throw Error("Receiver does not declare this private member");
    internal void Configure(object function, int declarationKind)
    {
        if (declarationKind == (int)JsPrivateKind.Method) method = (JsFunction)function;
        if (declarationKind == (int)JsPrivateKind.Getter) { getter = (JsFunction)function; getter.InferPropertyName("get " + name); }
        if (declarationKind == (int)JsPrivateKind.Setter) { setter = (JsFunction)function; setter.InferPropertyName("set " + name); }
    }
    internal object DecoratorFunction(string declaration)
    {
        if (declaration == "method") return method;
        if (kind != JsPrivateKind.AutoAccessor) return declaration == "getter" ? getter : setter;
        return declaration == "getter" ? getter ?? Js.PrivateDecoratorAccessor(this, name, false) : setter ?? Js.PrivateDecoratorAccessor(this, name, true);
    }
    internal void Decorate(string declaration, JsFunction function)
    {
        if (declaration == "method") method = function;
        else if (declaration == "getter") getter = function;
        else setter = function;
    }
    internal object ReadBacking(object receiver) { var cell = Storage(receiver); return cell.Initialized ? cell.Value : throw Error("Private backing storage is not initialized"); }
    internal object WriteBacking(object receiver, object value) { var cell = Storage(receiver); if (!cell.Initialized) throw Error("Private backing storage is not initialized"); cell.Value = value; return Js.Undefined; }
    internal bool Has(object receiver) => values.TryGetValue(Target(receiver), out _);
    internal object Read(object receiver)
    {
        var cell = Storage(receiver);
        if (kind == JsPrivateKind.Method) return method;
        if (kind is JsPrivateKind.Getter or JsPrivateKind.Setter) return getter is null ? throw Error("Private accessor has no getter") : getter.InvokeWithReceiver(receiver, Array.Empty<object>());
        if (kind == JsPrivateKind.AutoAccessor && getter != null) return getter.InvokeWithReceiver(receiver, Array.Empty<object>());
        return cell.Initialized ? cell.Value : throw Error("Private backing storage is not initialized");
    }
    internal object Write(object receiver, object value)
    {
        var cell = Storage(receiver);
        if (kind == JsPrivateKind.Method) throw Error("Private method is not writable");
        if (kind is JsPrivateKind.Getter or JsPrivateKind.Setter)
        {
            if (setter is null) throw Error("Private accessor has no setter");
            setter.InvokeWithReceiver(receiver, new[] { value });
        }
        else if (kind == JsPrivateKind.AutoAccessor && setter != null) setter.InvokeWithReceiver(receiver, new[] { value });
        else { if (!cell.Initialized) throw Error("Private backing storage is not initialized"); cell.Value = value; }
        return value;
    }
    internal void Initialize(object receiver, object value, int mode, bool inferName)
    {
        if (inferName) ((JsFunction)value).InferPropertyName(kind == JsPrivateKind.AutoAccessor ? "#" + name.TrimStart('#') + "_accessor_storage" : name);
        // Methods/accessors install their brand before fields. Auto-accessors
        // install the brand early but initialize their backing value in order.
        if (mode == 2)
        {
            var cell = Storage(receiver);
            if (cell.Initialized) throw Error("Private backing storage already exists");
            cell.Value = value; cell.Initialized = true; return;
        }
        if (!values.TryAdd(Target(receiver), new Cell(value, kind != JsPrivateKind.AutoAccessor))) throw Error("Private member is already installed");
    }
}

public static partial class Js
{
    internal static JsFunction PrivateDecoratorAccessor(JsPrivateMember member, string name, bool setter) => new CollectionMethod((setter ? "set " : "get ") + name, setter ? 1 : 0, (receiver, args) => setter ? member.WriteBacking(receiver, FunctionArgument(args,0)) : member.ReadBacking(receiver));
    public static object DefinePrivateMember(object home, string name, int kind, object function, object existing)
    {
        var member = existing as JsPrivateMember ?? new JsPrivateMember(name, (JsPrivateKind)kind);
        member.Configure(function, kind); return member;
    }
    public static void InitializePrivateMember(object receiver, object member, object value, int mode, bool inferName) => ((JsPrivateMember)member).Initialize(receiver, value, mode, inferName);
    public static object PrivateGet(object receiver, object member) => ((JsPrivateMember)member).Read(receiver);
    public static object PrivateSet(object receiver, object member, object value) => ((JsPrivateMember)member).Write(receiver, value);
    public static bool PrivateHas(object member, object receiver) => ((JsPrivateMember)member).Has(receiver);
}
