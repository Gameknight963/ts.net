using System;
using System.Globalization;

public static partial class Js
{
    sealed class DateTimeFormatValue : JsDescriptorObject
    {
        internal readonly DateLocaleSettings Settings;
        internal readonly JsFunction Format;
        NativeDateIntervals intervals;
        TsObject rangeOptions;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlDateTimeFormat.Constructor.Get("prototype");
        internal DateTimeFormatValue(object locales, object options)
        {
            var selected = SelectDateLocale(locales);
            Settings = new DateLocaleSettings(selected.Culture, options, "intl", selected.Locale);
            Format = new JsFunction((FunctionBody)(args => Text(FunctionArgument(args, 0))), null, "", 1);
        }
        static double Timestamp(object value)
        {
            double timestamp = IsUndefined(value) ? DateNow() : ClipDate(Number(value));
            if (double.IsNaN(timestamp)) throw new ArgumentOutOfRangeException(nameof(value), "Invalid time value");
            return timestamp;
        }
        internal string Text(object value) => Settings.Format(Timestamp(value));
        internal JsArray Parts(object value) => Settings.Parts(Timestamp(value));
        internal string Range(object start, object end)
        {
            if(IsUndefined(start) || IsUndefined(end))throw new InvalidOperationException("Range endpoints are required");
            double from=Number(start),to=Number(end);
            from=ClipDate(from);to=ClipDate(to);
            if(double.IsNaN(from) || double.IsNaN(to))throw new ArgumentOutOfRangeException(nameof(start),"Invalid date range");
            if(from==to)return Settings.Format(from);
            if(Settings.SameLocalDay(from,to)) {
                string first=Settings.Format(from);
                if(first==Settings.Format(to))return first;
            }
            // The legacy ICU interval API can collapse distinct fractional seconds.
            var options=rangeOptions ??= Settings.Options();
            if(options.Get("hourCycle") is "h11" or "h24")throw new NotSupportedException("Date ranges with h11 and h24 hour cycles are not supported by the native interval backend");
            if(options.Get("fractionalSecondDigits") is double && Math.Floor(from/1000)==Math.Floor(to/1000) && Settings.Format(from)!=Settings.Format(to))
                throw new NotSupportedException("Date ranges within one second are not supported with fractionalSecondDigits");
            intervals ??= new NativeDateIntervals(options,Settings.Culture.Name);
            return intervals.Range(from,to);
        }

    }
    static (CultureInfo Culture, LocaleValue Locale) SelectDateLocale(object locales)
    {
        var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
        LocaleValue locale = supported.Length() == 0 ? null : new LocaleValue(supported.ElementAt(0), Undefined);
        var culture = locale == null ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CollatorLocale(locale.BaseName));
        return (culture, locale);
    }
    static DateTimeFormatValue RequireDateTimeFormat(object receiver) => receiver as DateTimeFormatValue ?? throw new InvalidOperationException("DateTimeFormat receiver required");
    sealed class DateTimeFormatConstructor : NativeNamespaceFunction
    {
        internal DateTimeFormatConstructor() : base("DateTimeFormat", static args => new DateTimeFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1)), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.DateTimeFormat", false);
            prototype.DefineClassAccessor("format", new CollectionMethod("get format", 0, static (receiver, _) => RequireDateTimeFormat(receiver).Format), 1);
            prototype.DefineMethod("formatToParts", new CollectionMethod("formatToParts", 1, static (receiver, args) => RequireDateTimeFormat(receiver).Parts(FunctionArgument(args, 0))));
            prototype.DefineMethod("formatRange", new CollectionMethod("formatRange", 2, static (receiver, args) => RequireDateTimeFormat(receiver).Range(FunctionArgument(args,0),FunctionArgument(args,1))));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireDateTimeFormat(receiver).Settings.Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new DateTimeFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlDateTimeFormat { internal static readonly JsFunction Constructor = new DateTimeFormatConstructor(); }
}
