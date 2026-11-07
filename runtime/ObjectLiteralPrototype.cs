public static partial class Js
{
    public static void SetLiteralPrototype(object target, object prototype)
    {
        if (prototype == null || prototype is TsObject) ((TsObject)target).SetPrototype((TsObject)prototype);
    }
}
