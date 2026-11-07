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
            object options = FunctionArgument(args, 1);
            if (!IsNullish(options))
            {
                if (options is not TsObject bag) throw MakeThrown(new JsCaughtError("TypeError", "Clone options must be an object"));
                object transfer = bag.Get("transfer");
                if (!IsUndefined(transfer))
                {
                    object entries = Iteration.Start(transfer);
                    if (Iteration.Length(entries) != 0) throw new NotSupportedException("structuredClone transfer is not supported");
                }
            }
            return new StructuredCloner().Copy(args[0]);
        }), null, "structuredClone", 1);
    }

    sealed class StructuredCloner
    {
        readonly Dictionary<TsObject, TsObject> copies = new(ReferenceEqualityComparer.Instance);
        static Exception Uncloneable() => MakeThrown(new DomExceptionValue("Value cannot be cloned", "DataCloneError"));

        internal object Copy(object value)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            if (value is null or string or double or bool or BigInteger || IsUndefined(value)) return value;
            if (value is not TsObject source || source is JsFunction || source.IsModuleNamespace) throw Uncloneable();
            if (copies.TryGetValue(source, out var existing)) return existing;
            TsObject result;
            switch (source)
            {
                case BinaryBuffer buffer:
                    var bytes = new BinaryBuffer(buffer.Bytes.Length);
                    buffer.Bytes.CopyTo(bytes.Bytes, 0); result = bytes; break;
                case BinaryView view:
                    result = new BinaryView((BinaryBuffer)Copy(view.Buffer), view.Offset, view.Length); break;
                case TypedArrayValue array:
                    result = new TypedArrayValue(array.Name, (BinaryBuffer)Copy(array.Buffer), array.Offset, array.Count); break;
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
                    var errorCopy = new JsCaughtError(name, StringValue(error.Get("message")), error.HasOwn("message"));
                    copies.Add(source, errorCopy);
                    if (error.HasOwn("cause")) errorCopy.DefineHidden("cause", Copy(error.Get("cause")));
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
