using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

enum JsonScalarSnapshotStatus { Unavailable, NonScalar, Copied }

sealed partial class OwnProperties
{
    internal JsonScalarSnapshotStatus CopyJsonScalars(object[] names, Span<object> values)
    {
        if (symbolCount != 0 || Count != names.Length || values.Length < names.Length) return JsonScalarSnapshotStatus.Unavailable;
        int position = 0;
        for (int i = 0; i < inlineCount; i++) {
            var status = CopyJsonScalar(names, values, ref position, InlineKey(i), InlineValue(i));
            if (status != JsonScalarSnapshotStatus.Copied) return status;
        }
        if (tail != null) {
            if (tailOrder is List<string> order) {
                foreach (string key in order) {
                    var status = CopyJsonScalar(names, values, ref position, key, tail[key]);
                    if (status != JsonScalarSnapshotStatus.Copied) return status;
                }
            }
            else foreach (var item in tail) {
                var status = CopyJsonScalar(names, values, ref position, item.Key, item.Value);
                if (status != JsonScalarSnapshotStatus.Copied) return status;
            }
        }
        else if (tailOrder is SingleProperty single) {
            var status = CopyJsonScalar(names, values, ref position, single.Key, single.Value);
            if (status != JsonScalarSnapshotStatus.Copied) return status;
        }
        return position == names.Length ? JsonScalarSnapshotStatus.Copied : JsonScalarSnapshotStatus.Unavailable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static JsonScalarSnapshotStatus CopyJsonScalar(object[] names, Span<object> values, ref int position, string key, object stored)
    {
        if ((uint)position >= (uint)names.Length || key != (string)names[position] || Js.IsSymbolKey(key)) return JsonScalarSnapshotStatus.Unavailable;
        if (!IsJsonScalar(stored)) return JsonScalarSnapshotStatus.NonScalar;
        values[position++] = Read(stored);
        return JsonScalarSnapshotStatus.Copied;
    }
}

public abstract partial class TsObject
{
    internal JsonScalarSnapshotStatus CopyJsonScalars(object[] names, ref object[] values)
    {
        if (names.Length <= 4 || names.Length > 16 || GetType() != typeof(JsDynamicObject) || IsModuleNamespace ||
            ((JsDynamicObject)this).HasPropertyDescriptors || HasOwnAccessors) return JsonScalarSnapshotStatus.Unavailable;
        var properties = extra;
        if ((properties?.Count ?? 0) != names.Length) return JsonScalarSnapshotStatus.Unavailable;
        values ??= new object[16];
        return properties.CopyJsonScalars(names, values);
    }
}
