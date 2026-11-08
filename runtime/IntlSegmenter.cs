using System;
using System.Globalization;

public static partial class Js
{
    sealed class SegmenterValue : JsDescriptorObject
    {
        internal readonly string Locale;
        internal readonly string Granularity;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)IntlSegmenter.Constructor.Get("prototype");
        internal SegmenterValue(object locales, object optionValue)
        {
            var supported = (JsArray)CollatorSupportedLocales(locales, Undefined);
            Locale = supported.Length() == 0 ? CultureInfo.CurrentCulture.Name : CultureInfo.GetCultureInfo(CollatorLocale((string)supported.ElementAt(0))).Name;
            if (Locale.Length == 0) Locale = "en-US";
            TsObject options = IsUndefined(optionValue) ? null : ToObject(optionValue);
            string Option(string name, string fallback) {
                object value = options == null ? Undefined : options.Get(name);
                return IsUndefined(value) ? fallback : StringValue(value);
            }
            if (Option("localeMatcher", "best fit") is not ("lookup" or "best fit")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid locale matcher");
            Granularity = Option("granularity", "grapheme");
            if (Granularity is not ("grapheme" or "word" or "sentence")) throw new ArgumentOutOfRangeException(nameof(optionValue), "Invalid segmentation granularity");
            if (Granularity != "grapheme" && !NativeSegmentation.Available) throw new NotSupportedException("Word and sentence segmentation require system ICU");
        }
        internal object Options() {
            var result = new JsDynamicObject(); result.DefineOwn("locale", Locale); result.DefineOwn("granularity", Granularity); return result;
        }
    }
    sealed class SegmentsValue : JsDescriptorObject
    {
        internal readonly string Input;
        internal readonly int[] Starts;
        readonly bool[] Words;
        internal SegmentsValue(string input, SegmenterValue formatter)
        {
            Input = input;
            if (formatter.Granularity == "grapheme") Starts = StringInfo.ParseCombiningCharacters(input);
            else (Starts, Words) = NativeSegmentation.Segment(input, formatter.Locale, formatter.Granularity == "word");
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : SegmentPrototypes.Segments;
        internal object Data(int slot) {
            int start = Starts[slot], end = slot + 1 < Starts.Length ? Starts[slot + 1] : Input.Length;
            var result = new JsDynamicObject(); result.DefineOwn("segment", Input[start..end]); result.DefineOwn("index", (double)start); result.DefineOwn("input", Input);
            if (Words != null) result.DefineOwn("isWordLike", BoxBoolean(Words[slot]));
            return result;
        }
        internal object Containing(object value) {
            double index = Number(value); index = double.IsNaN(index) ? 0 : Math.Truncate(index);
            if (index < 0 || index >= Input.Length) return Undefined;
            int slot = Array.BinarySearch(Starts, (int)index);
            return Data(slot >= 0 ? slot : ~slot - 1);
        }
    }
    sealed class SegmentIterator(SegmentsValue segments) : JsDescriptorObject
    {
        int position;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : SegmentPrototypes.Iterator;
        internal object Next() {
            if (segments == null) return IteratorResult(Undefined, true);
            if (position >= segments.Starts.Length) { segments = null; return IteratorResult(Undefined, true); }
            object value = segments.Data(position++);
            if (position == segments.Starts.Length) segments = null;
            return IteratorResult(value, false);
        }
    }
    static class SegmentPrototypes
    {
        internal static readonly TsObject Segments = CreateSegments(), Iterator = CreateIterator();
        static TsObject CreateSegments() {
            var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
            result.DefineMethod("containing", new CollectionMethod("containing", 1, static (receiver, args) => receiver is SegmentsValue value ? value.Containing(FunctionArgument(args, 0)) : throw new InvalidOperationException("Segments receiver required")));
            result.DefineMethod(PropertyKey(IteratorSymbol), new CollectionMethod("[Symbol.iterator]", 0, static (receiver, _) => receiver is SegmentsValue value ? new SegmentIterator(value) : throw new InvalidOperationException("Segments receiver required")));
            return result;
        }
        static TsObject CreateIterator() {
            var result = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            result.DefineMethod("next", new CollectionMethod("next", 0, static (receiver, _) => receiver is SegmentIterator value ? value.Next() : throw new InvalidOperationException("Segment iterator receiver required")));
            result.DefineMethod(PropertyKey(ToStringTagSymbol), "Segmenter String Iterator", false);
            return result;
        }
    }
    sealed class SegmenterConstructor : NativeNamespaceFunction
    {
        internal SegmenterConstructor() : base("Segmenter", static _ => throw new InvalidOperationException("Segmenter requires new"), 0) {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Intl.Segmenter", false);
            prototype.DefineMethod("segment", new CollectionMethod("segment", 1, static (receiver, args) => receiver is SegmenterValue value ? new SegmentsValue(StringValue(FunctionArgument(args, 0)), value) : throw new InvalidOperationException("Segmenter receiver required")));
            prototype.DefineMethod("resolvedOptions", new CollectionMethod("resolvedOptions", 0, static (receiver, _) => receiver is SegmenterValue value ? value.Options() : throw new InvalidOperationException("Segmenter receiver required")));
            Member("supportedLocalesOf", new JsFunction((FunctionBody)(static args => CollatorSupportedLocales(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "supportedLocalesOf", 1));
        }
        public override object Construct(object[] args) => new SegmenterValue(FunctionArgument(args, 0), FunctionArgument(args, 1));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) {
            var prototype = ConstructionPrototype(newTarget);
            var result = (TsObject)Construct(args); result.SetPrototype(prototype); return result;
        }
    }
    static class IntlSegmenter { internal static readonly JsFunction Constructor = new SegmenterConstructor(); }
}
