using System;
using System.Globalization;

public static partial class Js
{
    sealed class DateTimeFormatValue : JsDescriptorObject
    {
        internal readonly DateLocaleSettings Settings;
        internal readonly JsFunction Format;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlDateTimeFormat.Constructor.Get("prototype");
        internal DateTimeFormatValue(object locales, object options)
        {
            var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
            LocaleValue locale = supported.Length() == 0 ? null : new LocaleValue(supported.ElementAt(0), Undefined);
            var culture = locale == null ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(CollatorLocale(locale.BaseName));
            Settings = new DateLocaleSettings(culture, options, "intl", locale);
            Format = new JsFunction((FunctionBody)(args => Text(FunctionArgument(args, 0))), null, "", 1);
        }
        internal string Text(object value)
        {
            double timestamp = IsUndefined(value) ? DateNow() : ClipDate(Number(value));
            if (double.IsNaN(timestamp)) throw new ArgumentOutOfRangeException(nameof(value), "Invalid time value");
            return Settings.Format(timestamp);
        }
    }
    static DateTimeFormatValue RequireDateTimeFormat(object receiver) => receiver as DateTimeFormatValue ?? throw new InvalidOperationException("DateTimeFormat receiver required");
    sealed class DateTimeFormatConstructor : NativeNamespaceFunction
    {
        internal DateTimeFormatConstructor() : base("DateTimeFormat", static args => new DateTimeFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1)), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.DateTimeFormat", false);
            prototype.DefineClassAccessor("format", new CollectionMethod("get format", 0, static (receiver, _) => RequireDateTimeFormat(receiver).Format), 1);
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireDateTimeFormat(receiver).Settings.Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new DateTimeFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
    static class IntlDateTimeFormat { internal static readonly JsFunction Constructor = new DateTimeFormatConstructor(); }
}
