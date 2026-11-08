using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

public static partial class Js
{
    static class StructuredCloneFunction
    {
        internal static readonly JsFunction Value = new((FunctionBody)(static args =>
        {
            if (args.Length == 0) throw MakeThrown(new JsCaughtError("TypeError", "structuredClone requires a value"));
            var cloner = new StructuredCloner();
            object options = FunctionArgument(args, 1);
            if (!IsNullish(options))
            {
                if (options is not TsObject bag) throw MakeThrown(new JsCaughtError("TypeError", "Clone options must be an object"));
                object transfer = bag.Get("transfer");
                if (!IsUndefined(transfer))
                {
                    cloner.PrepareTransfers(transfer);
                }
            }
            object result = cloner.Copy(args[0]);
            cloner.CompleteTransfers();
            return result;
        }), null, "structuredClone", 1);
    }

    sealed class StructuredCloner
    {
        readonly Dictionary<TsObject, TsObject> copies = new(ReferenceEqualityComparer.Instance);
        static Exception Uncloneable() => MakeThrown(new DomExceptionValue("Value cannot be cloned", "DataCloneError"));

        List<(BinaryBuffer Source, BinaryBuffer Target)> transfers;
        internal void PrepareTransfers(object value)
        {
            object iterator = Iteration.Start(value);
            var entries = new List<object>();
            int position = 0;
            while (position < Iteration.Length(iterator)) {
                entries.Add(Iteration.Value(iterator, position));
                position += Iteration.Step(iterator, position);
            }
            foreach (object entry in entries) {
                if (entry is not BinaryBuffer source || source is SharedBuffer || source.Detached || copies.ContainsKey(source)) throw Uncloneable();
                var target = new BinaryBuffer(0, source.Maximum);
                copies.Add(source, target);
                (transfers ??= new()).Add((source, target));
            }
        }
        internal void CompleteTransfers()
        {
            if (transfers == null) return;
            foreach (var transfer in transfers) if (transfer.Source.Detached) throw Uncloneable();
            foreach (var transfer in transfers) {
                transfer.Target.Bytes = transfer.Source.Bytes;
                transfer.Source.Detach();
            }
        }

        internal object Copy(object value)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            if (value is null or string or double or bool or BigInteger || IsUndefined(value)) return value;
            if (value is not TsObject source || source is JsFunction || source.IsModuleNamespace) throw Uncloneable();
            if (copies.TryGetValue(source, out var existing)) return existing;
            TsObject result;
            switch (source)
            {
                case CryptoKeyValue key:
                    result = new CryptoKeyValue(key.Bytes, key.Hash, key.Extractable, key.Usages, key.Name, key.Ec, key.Curve, key.PrivateKey, key.Rsa); break;
                case SharedBuffer shared:
                    result = new SharedBuffer(shared.Storage); break;
                case BinaryBuffer buffer:
                    if (buffer.Detached) throw Uncloneable();
                    var bytes = new BinaryBuffer(buffer.Bytes.Length, buffer.Maximum);
                    buffer.Bytes.CopyTo(bytes.Bytes, 0); result = bytes; break;
                case BinaryView view:
                    if (view.OutOfBounds) throw Uncloneable();
                    result = new BinaryView((BinaryBuffer)Copy(view.Buffer), view.Offset, view.FixedLength, view.Tracking); break;
                case TypedArrayValue array:
                    if (array.OutOfBounds) throw Uncloneable();
                    result = new TypedArrayValue(array.Name, (BinaryBuffer)Copy(array.Buffer), array.Offset, array.FixedCount, array.Tracking); break;
                case JsDate date: result = new JsDate(date.Timestamp); break;
                case JsRegExp regex:
                    result = new JsRegExp(regex.Pattern, regex.Flags, regex.Engine, regex.NamedGroups); break;
                case JsBoxedPrimitive boxed:
                    if (boxed.Value is JsSymbol) throw Uncloneable();
                    result = new JsBoxedPrimitive(boxed.Value); break;
                case DomExceptionValue exception:
                    result = new DomExceptionValue(exception.Message, exception.Name); break;
                case JsCaughtError error:
                    string name = error.Get("name") as string;
                    if (name is not ("EvalError" or "RangeError" or "ReferenceError" or "SyntaxError" or "TypeError" or "URIError")) name = "Error";
                    bool hasMessage = error.HasOwn("message") && !error.TryOwnAccessor("message", out _);
                    var errorCopy = new JsCaughtError(name, hasMessage ? StringValue(error.Get("message")) : "", hasMessage);
                    copies.Add(source, errorCopy);
                    if (error.HasOwn("cause") && !error.TryOwnAccessor("cause", out _)) errorCopy.DefineHidden("cause", Copy(error.Get("cause")));
                    errorCopy.DefineHidden("stack", error.Get("stack"));
                    return errorCopy;
                case FileValue file:
                    result = new FileValue(file.Bytes, file.Type, file.Name, file.Modified); break;
                case BlobValue blob:
                    // Blob bytes are immutable, so sharing this storage is safe.
                    result = new BlobValue(blob.Bytes, blob.Type); break;
                case JsKeyedCollection collection:
                    var target = new JsKeyedCollection(collection.IsSet);
                    copies.Add(source, target);
                    // Snapshot before recursively reading values, which may invoke getters.
                    var entries = new List<(object Key, object Value)>();
                    foreach (var entry in collection.Entries)
                        if (entry.Active) entries.Add((entry.Key, entry.Value));
                    foreach (var entry in entries) target.Write(Copy(entry.Key), Copy(entry.Value));
                    return target;
                case JsArray array:
                    var output = source is NumberArray ? new NumberArray(0) : new JsArray(0);
                    output.SetLength(array.Length()); result = output; break;
                case JsClassInstance or JsDynamicObject:
                    result = source.GetType().Assembly == typeof(TsObject).Assembly
                        ? new JsDynamicObject() : (TsObject)Activator.CreateInstance(source.GetType());
                    if (result is JsClassInstance instance) instance.ParentPrototype = DefaultClassObjectPrototype;
                    break;
                default:
                    // Generated data objects keep their CLR layout and structural interfaces.
                    // This calls their empty storage constructor, never a TS class constructor.
                    if (source.GetType().Assembly == typeof(TsObject).Assembly) throw Uncloneable();
                    result = (TsObject)Activator.CreateInstance(source.GetType()); break;
            }
            copies.Add(source, result);
            if (source is JsArray or JsClassInstance or JsDynamicObject || source.GetType().Assembly != typeof(TsObject).Assembly)
            {
                var keys = (JsArray)source.OwnKeys();
                for (uint i = 0; i < keys.Length(); i++)
                {
                    string key = (string)keys.ElementAt(i);
                    if (!IsSymbolKey(key) && source.HasOwn(key)) result.SetOwnValue(key, Copy(source.Get(key)));
                }
            }
            return result;
        }
    }
}
