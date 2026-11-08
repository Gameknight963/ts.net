using System;
using System.Globalization;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed class IcuModule(IntPtr library, string suffix, bool requiresCom)
    {
        internal bool Function<T>(string name, out T function) where T : Delegate
        {
            function = null;
            if (!NativeLibrary.TryGetExport(library, name + suffix, out var pointer)) return false;
            function = Marshal.GetDelegateForFunctionPointer<T>(pointer); return true;
        }
        internal IcuThreadScope Enter() => new(requiresCom);
    }
    readonly struct IcuThreadScope : IDisposable
    {
        readonly bool uninitialize;
        [DllImport("ole32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern void CoUninitialize();
        internal IcuThreadScope(bool required)
        {
            uninitialize = false;
            if (!required) return;
            int result = CoInitializeEx(IntPtr.Zero, 0);
            // An existing apartment is already initialized and must not be balanced here.
            if (result < 0 && result != unchecked((int)0x80010106)) Marshal.ThrowExceptionForHR(result);
            uninitialize = result >= 0;
        }
        public void Dispose() {if (uninitialize) CoUninitialize();}
    }
    static class PlatformIcu
    {
        static T Library<T>(string name, Func<IcuModule, T> bind, int major = 0, bool requiresCom = false) where T : class
        {
            IntPtr library;
            bool loaded = OperatingSystem.IsWindows()
                ? NativeLibrary.TryLoad(name, typeof(Js).Assembly, DllImportSearchPath.System32, out library)
                : NativeLibrary.TryLoad(name, out library);
            if (!loaded) return null;
            bool retained = false;
            try {
                var api = bind(new(library, "", requiresCom));
                if (api != null) {retained = true; return api;}
                // Linux distributions generally suffix both the SONAME and C exports.
                for (int version = major == 0 ? 255 : major; version >= (major == 0 ? 50 : major); version--) {
                    api = bind(new(library, "_" + version.ToString(CultureInfo.InvariantCulture), requiresCom));
                    if (api != null) {retained = true; return api;}
                }
                return null;
            }
            finally {
                // Bound APIs retain their platform module for the lifetime of the process.
                if (!retained) NativeLibrary.Free(library);
            }
        }
        internal static T Load<T>(Func<IcuModule, T> bind) where T : class
        {
            if (OperatingSystem.IsWindows())
                return Library("icu.dll", bind) ?? Library("icuuc.dll", bind, requiresCom: true) ?? Library("icuin.dll", bind, requiresCom: true);
            if (OperatingSystem.IsMacOS()) return Library("/usr/lib/libicucore.A.dylib", bind);
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD()) return null;
            var api = Library("libicuuc.so", bind) ?? Library("libicui18n.so", bind);
            if (api != null) return api;
            for (int version = 255; version >= 50; version--) {
                string suffix = version.ToString(CultureInfo.InvariantCulture);
                api = Library("libicuuc.so." + suffix, bind, version) ?? Library("libicui18n.so." + suffix, bind, version);
                if (api != null) return api;
            }
            return null;
        }
    }
}
