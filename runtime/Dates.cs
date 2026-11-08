using System;
using System.Globalization;

internal sealed class JsDate(double timestamp) : TsObject
{
    internal double Timestamp = timestamp;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.DatePrototype;
}

public static partial class Js
{
    internal static TsObject DatePrototype => (TsObject)RuntimeNamespaces.Date.Get("prototype");
    static double ClipDate(double value) => double.IsFinite(value) && Math.Abs(value) <= 8640000000000000d ? Math.Truncate(value) + 0d : double.NaN;
    static double DateNow() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static double ParseDate(string text) => TryParseIsoDate(text, out double iso) ? iso : DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? ClipDate(parsed.ToUnixTimeMilliseconds()) : double.NaN;
    static JsDate RequireDate(object receiver) => receiver as JsDate ?? throw new InvalidOperationException("Date method requires a date");
    static DateTimeOffset NativeDate(JsDate date) => NativeDate(date.Timestamp);
    static DateTimeOffset NativeDate(double timestamp)
    {
        try { return DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp); }
        catch (ArgumentOutOfRangeException) { throw new NotSupportedException("Date calendar formatting outside .NET's year range 1 through 9999 is not supported"); }
    }
    static string DateIso(object receiver)
    {
        var date = RequireDate(receiver);
        if (double.IsNaN(date.Timestamp)) throw new ArgumentOutOfRangeException(nameof(receiver), "Invalid time value");
        if (OutsideNativeDate(date.Timestamp)) return ExtendedDateIso(date.Timestamp);
        return NativeDate(date).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }
    static string DateText(object receiver)
    {
        var date = RequireDate(receiver);
        return double.IsNaN(date.Timestamp) ? "Invalid Date" : OutsideNativeDate(date.Timestamp) ? ExtendedDateText(date.Timestamp) : NativeDate(date).ToString("r", CultureInfo.InvariantCulture);
    }
    static JsDate CreateDate(object[] arguments)
    {
        if (arguments.Length > 1) return new JsDate(DateFromParts(arguments, false));
        if (arguments.Length == 0) return new JsDate(DateNow());
        object value = arguments[0];
        if (value is JsDate date) return new JsDate(date.Timestamp);
        if (value is TsObject obj) value = ToPrimitive(obj, "default");
        return new JsDate(value is string text ? ParseDate(text) : ClipDate(Number(value)));
    }
    sealed class DateConstructor : NativeNamespaceFunction
    {
        internal DateConstructor() : base("Date", static _ => LocalDateText(new JsDate(DateNow()), 2), 7)
        {
            Member("now", new JsFunction((FunctionBody)(static _ => DateNow()), (RawNumberFunction0)DateNow, "now", 0));
            Member("UTC", new JsFunction((FunctionBody)(static args => DateFromParts(args, true)), null, "UTC", 7));
            Member("parse", new JsFunction((FunctionBody)(static args => ParseDate(StringValue(FunctionArgument(args, 0)))), null, "parse", 1));
            var prototype = new JsCreatedObject(); UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToPrimitiveSymbol), new CollectionMethod("[Symbol.toPrimitive]", 1, static (receiver, args) =>
            {
                if (receiver is not TsObject value) throw new InvalidOperationException("Date primitive conversion requires an object");
                object hint = FunctionArgument(args, 0);
                if (hint is not string text || text is not ("default" or "string" or "number")) throw new InvalidOperationException("Invalid primitive conversion hint");
                return OrdinaryPrimitive(value, text != "number");
            }), false);
            DefineDateGetters(prototype);
            DefineDateSetters(prototype);
            DefineDateLocaleFormatting(prototype);
            var getTime = new CollectionMethod("getTime", 0, (receiver, _) => RequireDate(receiver).Timestamp);
            prototype.DefineMethod("getTime", getTime);
            prototype.DefineMethod("valueOf", new CollectionMethod("valueOf", 0, (receiver, _) => RequireDate(receiver).Timestamp));
            prototype.DefineMethod("setTime", new CollectionMethod("setTime", 1, (receiver, args) => RequireDate(receiver).Timestamp = ClipDate(Number(FunctionArgument(args, 0)))));
            prototype.DefineMethod("toISOString", new CollectionMethod("toISOString", 0, (receiver, _) => DateIso(receiver)));
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) => LocalDateText(receiver, 2)));
            prototype.DefineMethod("toDateString", new CollectionMethod("toDateString", 0, (receiver, _) => LocalDateText(receiver, 0)));
            prototype.DefineMethod("toTimeString", new CollectionMethod("toTimeString", 0, (receiver, _) => LocalDateText(receiver, 1)));
            var utc = new CollectionMethod("toUTCString", 0, (receiver, _) => DateText(receiver)); prototype.DefineMethod("toUTCString", utc); prototype.DefineMethod("toGMTString", utc);
            prototype.DefineMethod("toJSON", new CollectionMethod("toJSON", 1, (receiver, _) =>
            {
                var source = ToObject(receiver); object value = ToPrimitive(source, "number");
                return value is double number && !double.IsFinite(number) ? null : CallMethod0(source.Get("toISOString"), source);
            }));
        }
        public override object Construct(object[] arguments) => CreateDate(arguments);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) { var result = CreateDate(arguments); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
