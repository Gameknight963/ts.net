global using ObjectArrayStorage = ArrayStorage<object, ObjectArrayMissing>;
global using NumberArrayStorage = ArrayStorage<double, NumberArrayMissing>;

interface IArrayMissing<T>
{
    static abstract T Missing { get; }
}
readonly struct ObjectArrayMissing : IArrayMissing<object>
{
    public static object Missing => JsArray.HoleValue();
}
readonly struct NumberArrayMissing : IArrayMissing<double>
{
    public static double Missing => NumberArray.MissingValue;
}
