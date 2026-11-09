using System;
using System.Numerics;
using System.Collections.Generic;

public static partial class Js
{
    sealed partial class NumberFormatValue : JsDescriptorObject
    {
        readonly NumberLocaleSettings settings;
        internal readonly JsFunction Format;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlNumberFormat.Constructor.Get("prototype");
        internal NumberFormatValue(object locales, object options)
        {
            settings = new NumberLocaleSettings(locales, options);
            Format = new JsFunction((FunctionBody)(args => Text(FunctionArgument(args, 0))), null, "", 1);
        }
        internal string DecimalSeparator => settings.culture.NumberFormat.NumberDecimalSeparator;
        internal string Text(object value) => settings.LocalizeDigits(LatinText(value));
        string LatinText(object value, bool includeAffix = true, List<string> compactLabels = null)
        {
            value = IntlNumericValue(value);
            if (value is LocaleDecimal exact) return settings.FormatLatin(exact, true, includeAffix, compactLabels);
            if (value is BigInteger integer) return settings.FormatLatin(integer, true, includeAffix, compactLabels);
            double number = Number(value);
            return settings.FormatLatin(number, double.IsFinite(number), includeAffix, compactLabels);
        }
        internal object Parts(object value)
        {
            var compactLabels=settings.UsesNativeCompact ? new List<string>() : null;
            string text = LatinText(value, false,compactLabels);
            var info = settings.culture.NumberFormat;
            var result = new JsArray(0);
            JsDynamicObject previousLiteral=null;
            void Part(string type, string contents)
            {
                if(type=="literal" && previousLiteral!=null){previousLiteral.Set("value",(string)previousLiteral.Get("value")+contents);return;}
                var part = new JsDynamicObject();previousLiteral=type=="literal" ? part : null; part.DefineOwn("type", type); part.DefineOwn("value", type is "integer" or "fraction" or "exponentInteger" ? settings.LocalizeDigits(contents) : contents); result.Append(part);
            }
            int position = 0; bool fraction = false, exponent = false;
            while (position < text.Length)
            {
                bool Token(string token, string type)
                {
                    if (token.Length == 0 || !text.AsSpan(position).StartsWith(token, StringComparison.Ordinal)) return false;
                    Part(type, token); position += token.Length; return true;
                }
                bool Affix(string token,string type) {
                    if(token.Length==0 || !text.AsSpan(position).StartsWith(token,StringComparison.Ordinal))return false;
                    int prefix=0;while(prefix<token.Length && char.GetUnicodeCategory(token[prefix])==System.Globalization.UnicodeCategory.Format)prefix++;
                    if(prefix!=0)Part("literal",token[..prefix]);
                    int end=token.Length;while(end>prefix && char.GetUnicodeCategory(token[end-1])==System.Globalization.UnicodeCategory.Format)end--;
                    if(prefix<end)Part(type,token[prefix..end]);if(end<token.Length)Part("literal",token[end..]);position+=token.Length;return true;
                }
                bool compactToken=false;
                if(compactLabels!=null)foreach(string label in compactLabels)if(Token(label,"compact")){compactToken=true;break;}
                if(compactToken)continue;
                if (settings.style == "currency" && !settings.CurrencyName && Affix(info.CurrencySymbol, "currency")) continue;
                if (!exponent && settings.notation is ("scientific" or "engineering") && Token(settings.exponentSeparator, "exponentSeparator")) { exponent = true; fraction = false; continue; }
                if (settings.notation == "compact" && (Token(settings.CompactLabel(3), "compact") || Token(settings.CompactLabel(6), "compact") || Token(settings.CompactLabel(9), "compact") || Token(settings.CompactLabel(12), "compact"))) continue;
                if (exponent && Affix(info.NegativeSign, "exponentMinusSign")) continue;
                if (Token(info.NumberDecimalSeparator, "decimal")) { fraction = true; continue; }
                if (settings.style == "percent" && Affix(info.PercentSymbol, settings.notation == "compact" ? "unit" : "percentSign")) continue;
                bool group=position+info.NumberGroupSeparator.Length<text.Length && char.IsAsciiDigit(text[position+info.NumberGroupSeparator.Length]) && Token(info.NumberGroupSeparator,"group");
                if (group || Affix(info.NegativeSign, "minusSign") || Affix(info.PositiveSign, "plusSign")
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
            if (settings.CurrencyName) { Part("literal", " "); Part("currency", settings.CurrencyNameAffix(text)); }
            if (settings.style == "unit") { var affix = settings.UnitAffix(text); if (affix.Gap.Length != 0) Part("literal", affix.Gap); Part("unit", affix.Name); }
            return result;
        }
        internal object Options()
        {
            var result = new JsDynamicObject();
            result.DefineOwn("locale", settings.localeName); result.DefineOwn("numberingSystem", settings.numberingSystem);
            result.DefineOwn("style", settings.style);
            if (settings.style == "currency")
            {
                result.DefineOwn("currency", settings.currency); result.DefineOwn("currencyDisplay", settings.currencyDisplay); result.DefineOwn("currencySign", settings.currencySign);
            }
            if (settings.style == "unit") { result.DefineOwn("unit", settings.unit); result.DefineOwn("unitDisplay", settings.unitDisplay); }
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
            if (settings.notation == "compact") result.DefineOwn("compactDisplay", settings.compactDisplay);
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
            prototype.DefineMethod("formatRange", new CollectionMethod("formatRange", 2, static (receiver, args) => RequireNumberFormat(receiver).Range(FunctionArgument(args, 0), FunctionArgument(args, 1), false)));
            prototype.DefineMethod("formatRangeToParts", new CollectionMethod("formatRangeToParts", 2, static (receiver, args) => RequireNumberFormat(receiver).Range(FunctionArgument(args, 0), FunctionArgument(args, 1), true)));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => RequireNumberFormat(receiver).Options()));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new NumberFormatValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlNumberFormat { internal static readonly JsFunction Constructor = new NumberFormatConstructor(); }
}
