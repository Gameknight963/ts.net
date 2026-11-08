using System;
using System.Numerics;

public static partial class Js
{
    sealed class NumberFormatValue : JsDescriptorObject
    {
        readonly NumberLocaleSettings settings;
        internal readonly JsFunction Format;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlNumberFormat.Constructor.Get("prototype");
        internal NumberFormatValue(object locales, object options)
        {
            settings = new NumberLocaleSettings(locales, options);
            Format = new JsFunction((FunctionBody)(args => Text(FunctionArgument(args, 0))), null, "", 1);
        }
        internal string Text(object value) => value is BigInteger integer ? settings.Format(integer, true) : FormatNumber(Number(value));
        string FormatNumber(double value) => settings.Format(value, double.IsFinite(value));
        internal object Parts(object value)
        {
            string text = Text(value);
            var info = settings.culture.NumberFormat;
            var result = new JsArray(0);
            void Part(string type, string contents)
            {
                var part = new JsDynamicObject(); part.DefineOwn("type", type); part.DefineOwn("value", contents); result.Append(part);
            }
            int position = 0; bool fraction = false, exponent = false;
            while (position < text.Length)
            {
                bool Token(string token, string type)
                {
                    if (token.Length == 0 || !text.AsSpan(position).StartsWith(token, StringComparison.Ordinal)) return false;
                    Part(type, token); position += token.Length; return true;
                }
                if (settings.style == "currency" && Token(info.CurrencySymbol, "currency")) continue;
                if (!exponent && settings.notation != "standard" && Token("E", "exponentSeparator")) { exponent = true; fraction = false; continue; }
                if (exponent && Token(info.NegativeSign, "exponentMinusSign")) continue;
                if (Token(info.NumberDecimalSeparator, "decimal")) { fraction = true; continue; }
                if (settings.style == "percent" && Token(info.PercentSymbol, "percentSign")) continue;
                if (Token(info.NumberGroupSeparator, "group") || Token(info.NegativeSign, "minusSign") || Token(info.PositiveSign, "plusSign")
                    || Token(info.NaNSymbol, "nan") || Token(info.PositiveInfinitySymbol, "infinity")) continue;
                int start = position;
                if (char.IsAsciiDigit(text[position]))
                {
                    while (position < text.Length && char.IsAsciiDigit(text[position])) position++;
                    Part(exponent ? "exponentInteger" : fraction ? "fraction" : "integer", text[start..position]);
                    if (exponent) exponent = false;
                }
                else { Part("literal", text.Substring(position, 1)); position++; }
            }
            return result;
        }
        internal object Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", settings.culture.Name); result.DefineOwn("numberingSystem", "latn");
            result.DefineOwn("style", settings.style);
            if (settings.style == "currency")
            {
                result.DefineOwn("currency", settings.currency); result.DefineOwn("currencyDisplay", settings.currencyDisplay); result.DefineOwn("currencySign", settings.currencySign);
            }
            result.DefineOwn("minimumIntegerDigits", (double)settings.integerDigits);
            if (settings.maximumSignificant == 0 || settings.roundingPriority != "auto")
            {
                result.DefineOwn("minimumFractionDigits", (double)settings.minimum); result.DefineOwn("maximumFractionDigits", (double)settings.maximum);
            }
            if (settings.maximumSignificant != 0)
            {
                result.DefineOwn("minimumSignificantDigits", (double)settings.minimumSignificant);
                result.DefineOwn("maximumSignificantDigits", (double)settings.maximumSignificant);
            }
            result.DefineOwn("useGrouping", settings.groupingOption); result.DefineOwn("notation", settings.notation);
            result.DefineOwn("signDisplay", settings.signDisplay);
            result.DefineOwn("roundingIncrement", (double)settings.roundingIncrement); result.DefineOwn("roundingMode", settings.roundingMode);
            result.DefineOwn("roundingPriority", settings.roundingPriority); result.DefineOwn("trailingZeroDisplay", settings.trailingZeroDisplay);
            return result;
        }
    }
    static NumberFormatValue RequireNumberFormat(object receiver) => receiver as NumberFormatValue ?? throw new InvalidOperationException("NumberFormat receiver required");
    sealed class NumberFormatConstructor : NativeNamespaceFunction
    {
        internal NumberFormatConstructor() : base("NumberFormat", static args => new NumberFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1)), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.NumberFormat", false);
            prototype.DefineClassAccessor("format", new CollectionMethod("get format", 0, static (receiver, _) => RequireNumberFormat(receiver).Format), 1);
            prototype.DefineMethod("formatToParts", new CollectionMethod("formatToParts", 1, static (receiver, args) => RequireNumberFormat(receiver).Parts(FunctionArgument(args, 0))));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireNumberFormat(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new NumberFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
    static class IntlNumberFormat { internal static readonly JsFunction Constructor = new NumberFormatConstructor(); }
}
