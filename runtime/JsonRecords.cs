using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class JsonRecordShape(string[] names, TsObject prototype, uint references = 0)
    {
        internal readonly string[] Names = names;
        internal readonly TsObject Prototype = prototype;
        internal readonly uint References = references;
        readonly Dictionary<string, int> indices = CreateIndices(names);
        static Dictionary<string, int> CreateIndices(string[] names)
        {
            var result = new Dictionary<string, int>(names.Length, StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++) result.Add(names[i], i);
            return result;
        }
        internal int Index(string key) => indices.TryGetValue(key, out int index) ? index : -1;
    }

    class JsonNumericRecord : JsNumericClassInstance
    {
        protected readonly JsonRecordShape Shape;
        readonly double[] values;
        internal JsonNumericRecord(JsonRecordShape shape, double[] values)
        {
            Shape = shape;
            this.values = values;
            ulong mask = values.Length == 32 ? ulong.MaxValue : (1ul << (values.Length * 2)) - 1;
            InitializeDirectClassWide(null, (int)mask, (int)(mask >> 32));
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : Shape.Prototype;
        internal override bool HasInProperty(string key) => HasOwn(key) || PropertyPrototype != null && PropertyPrototype.HasInProperty(key);
        public override bool HasProperty(string key) => HasOwn(key) || PropertyPrototype != null && PropertyPrototype.HasProperty(key);
        protected override int NumericFieldIndex(string key) => Shape.Index(key);
        protected override double ReadNumericField(int index) => values[index];
        protected override void WriteNumericField(int index, double value) => values[index] = value;
        protected override void CopyNumericFields()
        {
            for (int i = 0; i < values.Length; i++) CopyNumericField(i, Shape.Names[i]);
        }
    }

    // Numeric-only records retain their smaller instance layout. Reference
    // fields use a separate subclass and the same materialization fallback.
    sealed class JsonMixedRecord(JsonRecordShape shape, double[] numbers, object[] references)
        : JsonNumericRecord(shape, numbers)
    {
        protected override bool IsReferenceField(int index) => (Shape.References & (1u << index)) != 0;
        protected override object ReadReferenceField(int index) => references[index];
        protected override void WriteReferenceField(int index, object value) => references[index] = value;
        protected override void CopyNumericFields()
        {
            base.CopyNumericFields();
            Array.Clear(references);
        }
    }
}
