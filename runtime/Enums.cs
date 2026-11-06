public static partial class Js
{
    internal static TsObject EnumObjectPrototype => DefaultClassObjectPrototype;
    public static void InitializeEnum(object value) => ((TsObject)value).SetPrototype(EnumObjectPrototype);
}
