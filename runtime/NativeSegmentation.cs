using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    static class NativeSegmentation
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr OpenIterator(int kind, [MarshalAs(UnmanagedType.LPUTF8Str)] string locale, IntPtr text, int length, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int MoveIterator(IntPtr iterator);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseIterator(IntPtr iterator);
        sealed record Api(IcuModule Module, OpenIterator Open, MoveIterator First, MoveIterator Next, MoveIterator Status, CloseIterator Close);
        static readonly Lazy<Api> Backend = new(() => PlatformIcu.Load(Bind));
        internal static bool Available => Backend.Value != null;
        static Api Bind(IcuModule module)
        {
            if (!module.Function<OpenIterator>("ubrk_open", out var open) ||
                !module.Function<MoveIterator>("ubrk_first", out var first) ||
                !module.Function<MoveIterator>("ubrk_next", out var next) ||
                !module.Function<MoveIterator>("ubrk_getRuleStatus", out var status) ||
                !module.Function<CloseIterator>("ubrk_close", out var close)) return null;
            return new(module, open, first, next, status, close);
        }
        internal static (int[] Starts, bool[] Words) Segment(string input, string locale, bool word)
        {
            var api = Backend.Value ?? throw new NotSupportedException("Word and sentence segmentation require system ICU");
            if (input.Length == 0) return (Array.Empty<int>(), word ? Array.Empty<bool>() : null);
            GCHandle pin = default;
            using var thread = api.Module.Enter();
            IntPtr iterator = IntPtr.Zero;
            try {
                pin = GCHandle.Alloc(input, GCHandleType.Pinned);
                int error = 0;
                iterator = api.Open(word ? 1 : 3, locale, pin.AddrOfPinnedObject(), input.Length, ref error);
                if (error > 0 || iterator == IntPtr.Zero)
                    throw new InvalidOperationException("Native segmentation failed: ICU status " + error);
                var starts = new List<int>(); var words = word ? new List<bool>() : null;
                int start = api.First(iterator), end;
                while ((end = api.Next(iterator)) != -1) {
                    if (end <= start || end > input.Length) throw new InvalidOperationException("Invalid native segmentation boundary");
                    starts.Add(start);
                    if (word) words.Add(api.Status(iterator) >= 100);
                    start = end;
                }
                return (starts.ToArray(), words?.ToArray());
            }
            finally {
                try {if (iterator != IntPtr.Zero) api.Close(iterator);}
                finally {
                    if (pin.IsAllocated) pin.Free();
                }
            }
        }
    }
}
