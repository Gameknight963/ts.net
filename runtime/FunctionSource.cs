using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FunctionSourceAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

public partial class JsFunction
{
    internal virtual string SourceText => Js.SourceOf(body);
}

public static partial class Js
{
    sealed record SourceTextCache(string Text);
    static readonly ConditionalWeakTable<MethodInfo, SourceTextCache> functionSourceText = new();
    internal static string SourceOf(Delegate body) => body == null ? null : functionSourceText.GetValue(body.Method,
        static method => new(method.GetCustomAttribute<FunctionSourceAttribute>()?.Text)).Text;
}
