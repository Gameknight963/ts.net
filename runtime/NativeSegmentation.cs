using System;
using System.Collections.Generic;
using System.Globalization;
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
        [DllImport("ole32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern void CoUninitialize();
        sealed record Api(OpenIterator Open, MoveIterator First, MoveIterator Next, MoveIterator Status, CloseIterator Close, bool RequiresCom);
        static readonly Lazy<Api> Backend = new(Load);
        internal static bool Available => Backend.Value != null;
        static bool Bind(IntPtr library, string suffix, bool requiresCom, out Api api)
        {
            api = null;
            if (!NativeLibrary.TryGetExport(library, "ubrk_open" + suffix, out var open) ||
                !NativeLibrary.TryGetExport(library, "ubrk_first" + suffix, out var first) ||
                !NativeLibrary.TryGetExport(library, "ubrk_next" + suffix, out var next) ||
                !NativeLibrary.TryGetExport(library, "ubrk_getRuleStatus" + suffix, out var status) ||
                !NativeLibrary.TryGetExport(library, "ubrk_close" + suffix, out var close)) return false;
            api = new(Marshal.GetDelegateForFunctionPointer<OpenIterator>(open),
                Marshal.GetDelegateForFunctionPointer<MoveIterator>(first),
                Marshal.GetDelegateForFunctionPointer<MoveIterator>(next),
                Marshal.GetDelegateForFunctionPointer<MoveIterator>(status),
                Marshal.GetDelegateForFunctionPointer<CloseIterator>(close), requiresCom);
            return true;
        }
        static Api Library(string name, int major = 0, bool requiresCom = false)
        {
            IntPtr library;
            bool loaded = OperatingSystem.IsWindows()
                ? NativeLibrary.TryLoad(name, typeof(Js).Assembly, DllImportSearchPath.System32, out library)
                : NativeLibrary.TryLoad(name, out library);
            if (!loaded) return null;
            bool retained = false;
            try {
                if (Bind(library, "", requiresCom, out var api)) {retained = true; return api;}
                // Linux distributions generally suffix both the SONAME and C exports.
                for (int version = major == 0 ? 255 : major; version >= (major == 0 ? 50 : major); version--)
                    if (Bind(library, "_" + version.ToString(CultureInfo.InvariantCulture), requiresCom, out api)) {retained = true; return api;}
                return null;
            }
            finally {
                // A successful binding keeps this platform module loaded for the process.
                if (!retained) NativeLibrary.Free(library);
            }
        }
        static Api Load()
        {
            if (OperatingSystem.IsWindows())
                return Library("icu.dll") ?? Library("icuuc.dll", requiresCom: true) ?? Library("icuin.dll", requiresCom: true);
            if (OperatingSystem.IsMacOS()) return Library("/usr/lib/libicucore.A.dylib");
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD()) return null;
            var api = Library("libicuuc.so") ?? Library("libicui18n.so");
            if (api != null) return api;
            for (int version = 255; version >= 50; version--) {
                string suffix = version.ToString(CultureInfo.InvariantCulture);
                api = Library("libicuuc.so." + suffix, version) ?? Library("libicui18n.so." + suffix, version);
                if (api != null) return api;
            }
            return null;
        }
        internal static (int[] Starts, bool[] Words) Segment(string input, string locale, bool word)
        {
            var api = Backend.Value ?? throw new NotSupportedException("Word and sentence segmentation require system ICU");
            if (input.Length == 0) return (Array.Empty<int>(), word ? Array.Empty<bool>() : null);
            GCHandle pin = default;
            bool uninitialize = false;
            IntPtr iterator = IntPtr.Zero;
            try {
                if (api.RequiresCom) {
                    int result = CoInitializeEx(IntPtr.Zero, 0);
                    // A pre-existing apartment is already initialized and must not be balanced here.
                    if (result < 0 && result != unchecked((int)0x80010106)) Marshal.ThrowExceptionForHR(result);
                    uninitialize = result >= 0;
                }
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
                    if (uninitialize) CoUninitialize();
                }
            }
        }
    }
}
