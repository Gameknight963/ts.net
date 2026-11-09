using System;
using System.Buffers;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed class NativeDisplayNames
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr OpenNames([MarshalAs(UnmanagedType.LPUTF8Str)] string locale, [In] int[] contexts, int count, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseNames(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int GetName(NameHandle handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string code,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] buffer, int capacity, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int GetKeyName(NameHandle handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, [MarshalAs(UnmanagedType.LPUTF8Str)] string code,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] buffer, int capacity, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr CurrencyName(IntPtr code, [MarshalAs(UnmanagedType.LPUTF8Str)] string locale,
            int style, out byte choice, out int length, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr OpenDate([MarshalAs(UnmanagedType.LPUTF8Str)] string locale, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int GetDateName(NameHandle handle, int field, int width,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] buffer, int capacity, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int ScriptCodes([MarshalAs(UnmanagedType.LPUTF8Str)] string code, [Out] int[] values, int capacity, ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ScriptCodeName(int value);
        sealed record Api(IcuModule Module, OpenNames Open, CloseNames Close, GetName Language, GetName BareLanguage, GetName Variant, GetName Region, GetName Script,
            GetKeyName Key, CurrencyName Currency, OpenDate DateOpen, CloseNames DateClose, GetDateName DateName, ScriptCodes Scripts, ScriptCodeName ScriptCode);
        sealed class NameHandle(IcuModule module, CloseNames close) : SafeHandle(IntPtr.Zero, true)
        {
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr value) => SetHandle(value);
            protected override bool ReleaseHandle()
            {
                try {using var thread = module.Enter(); close(handle); return true;}
                catch {return false;}
            }
        }
        static readonly Lazy<Api> Backend = new(() => PlatformIcu.Load(Bind));
        static Api Bind(IcuModule module)
        {
            if (!module.Function<OpenNames>("uldn_openForContext", out var open) ||
                !module.Function<CloseNames>("uldn_close", out var close) ||
                !module.Function<GetName>("uldn_localeDisplayName", out var language) ||
                !module.Function<GetName>("uldn_languageDisplayName", out var bareLanguage) ||
                !module.Function<GetName>("uldn_variantDisplayName", out var variant) ||
                !module.Function<GetName>("uldn_regionDisplayName", out var region) ||
                !module.Function<GetName>("uldn_scriptDisplayName", out var script) ||
                !module.Function<GetKeyName>("uldn_keyValueDisplayName", out var key) ||
                !module.Function<CurrencyName>("ucurr_getName", out var currency)) return null;
            module.Function<OpenDate>("udatpg_open", out var dateOpen);
            module.Function<CloseNames>("udatpg_close", out var dateClose);
            module.Function<GetDateName>("udatpg_getFieldDisplayName", out var dateName);
            module.Function<ScriptCodes>("uscript_getCode", out var scripts);
            module.Function<ScriptCodeName>("uscript_getShortName", out var scriptCode);
            return new(module, open, close, language, bareLanguage, variant, region, script, key, currency, dateOpen, dateClose, dateName, scripts, scriptCode);
        }
        readonly Api api;
        readonly NameHandle handle;
        readonly NameHandle fullHandle;
        readonly string locale, type, style, fallback;
        internal NativeDisplayNames(string locale, string type, string style, string fallback, string languageDisplay)
        {
            api = Backend.Value ?? throw new NotSupportedException("Display names require system ICU");
            this.locale = locale; this.type = type; this.style = style; this.fallback = fallback;
            if (type == "currency") return;
            bool date = type == "dateTimeField";
            if (date && (api.DateOpen == null || api.DateClose == null || api.DateName == null))
                throw new NotSupportedException("Date-time field names require a newer system ICU");
            var created = new NameHandle(api.Module, date ? api.DateClose : api.Close);
            try {
                using var thread = api.Module.Enter();
                int error = 0;
                if (date) created.Initialize(api.DateOpen(locale, ref error));
                else {
                    int[] contexts = [languageDisplay == "dialect" ? 1 : 0, style == "long" ? 0x200 : 0x201, fallback == "none" ? 0x301 : 0x300];
                    created.Initialize(api.Open(locale, contexts, contexts.Length, ref error));
                }
                if (error > 0 || created.IsInvalid) throw new InvalidOperationException("Native display names failed: ICU status " + error);
                handle = created;
            }
            catch {created.Dispose(); throw;}
            if (!date && fallback == "none") {
                if (style == "long") fullHandle = handle;
                else {
                    var full = new NameHandle(api.Module, api.Close);
                    try {
                        using var thread = api.Module.Enter();
                        int error = 0;
                        int[] contexts = [languageDisplay == "dialect" ? 1 : 0, 0x200, 0x301];
                        full.Initialize(api.Open(locale, contexts, contexts.Length, ref error));
                        if (error > 0 || full.IsInvalid) throw new InvalidOperationException("Native display names failed: ICU status " + error);
                        fullHandle = full;
                    }
                    catch {full.Dispose(); handle.Dispose(); throw;}
                }
            }
        }
        string Get(GetName function, NameHandle selected, string code) => Read(buffer => {
            int error = 0;
            int length = function(selected, code, buffer, buffer.Length, ref error);
            return (length, error);
        });
        bool KnownScript(string code)
        {
            // A real script label can equal its code, for example "Thai".
            if (api.Scripts == null || api.ScriptCode == null) return false;
            int error = 0; var values = new int[1];
            int count = api.Scripts(code, values, values.Length, ref error);
            return error <= 0 && count == 1 && Marshal.PtrToStringUTF8(api.ScriptCode(values[0])) == code;
        }
        bool KnownLanguage(string code)
        {
            // Some platform ICU shims ignore the no-substitute context.
            // Check components before accepting a synthesized locale label.
            string[] parts = code.Split('_');
            string name = Get(api.BareLanguage, fullHandle, parts[0]);
            if (name == null || name == parts[0]) return false;
            int next = 1;
            if (next < parts.Length && parts[next].Length == 4 && AsciiLetters(parts[next])) {
                name = Get(api.Script, fullHandle, parts[next]);
                if (name == null || name == parts[next] && !KnownScript(parts[next])) return false;
                next++;
            }
            if (next < parts.Length && (parts[next].Length == 2 || parts[next].Length == 3 && AsciiDigits(parts[next]))) {
                name = Get(api.Region, fullHandle, parts[next]);
                if (name == null || name == parts[next]) return false;
                next++;
            }
            for (; next < parts.Length; next++) {
                string variant = parts[next].ToUpperInvariant();
                name = Get(api.Variant, fullHandle, variant);
                if (name == null || name == variant) return false;
            }
            return true;
        }
        static string Read(Func<char[], (int Length, int Error)> read)
        {
            char[] buffer = ArrayPool<char>.Shared.Rent(128);
            try {
                while (true) {
                    var result = read(buffer);
                    if (result.Error == 15 && result.Length >= buffer.Length) {
                        int length = checked(result.Length + 1);
                        var larger = ArrayPool<char>.Shared.Rent(length);
                        ArrayPool<char>.Shared.Return(buffer); buffer = larger; continue;
                    }
                    if (result.Error == 1 || result.Length == 0) return null;
                    if (result.Error > 0 || result.Length < 0 || result.Length > buffer.Length)
                        throw new InvalidOperationException("Native display name failed: ICU status " + result.Error);
                    return new string(buffer, 0, result.Length);
                }
            }
            finally {ArrayPool<char>.Shared.Return(buffer);}
        }
        internal string Name(string code, int field = 0)
        {
            using var thread = api.Module.Enter();
            if (type == "currency") {
                int error = 0;
                // Unknown currencies can return the caller's input pointer.
                IntPtr input=Marshal.StringToHGlobalUni(code);
                try {
                    IntPtr value = api.Currency(input, locale, 1, out byte choice, out int length, ref error);
                    if (error > 0 || value == IntPtr.Zero || value==input) return null;
                    if (choice != 0) throw new NotSupportedException("Legacy ICU currency choice formats are not supported");
                    if(length<0 || length>4096)throw new InvalidOperationException("Invalid native currency name length");
                    string currency = Marshal.PtrToStringUni(value, length);
                    return currency == code ? null : currency;
                }finally{Marshal.FreeHGlobal(input);}
            }
            if (fallback == "none" && type == "language" && !KnownLanguage(code)) return null;
            string result = Read(buffer => {
                int error = 0, length;
                if (type == "dateTimeField") length = api.DateName(handle, field, style == "long" ? 0 : style == "short" ? 1 : 2, buffer, buffer.Length, ref error);
                else if (type == "calendar") length = api.Key(handle, "calendar", code, buffer, buffer.Length, ref error);
                else {
                    var name = type == "language" ? api.Language : type == "region" ? api.Region : api.Script;
                    length = name(handle, code, buffer, buffer.Length, ref error);
                }
                return (length, error);
            });
            if (fallback == "none" && result == code && type is "script" or "region" or "calendar") {
                string full = type == "calendar" ? Read(buffer => {
                    int error = 0; int length = api.Key(fullHandle, "calendar", code, buffer, buffer.Length, ref error);
                    return (length, error);
                }) : Get(type == "script" ? api.Script : api.Region, fullHandle, code);
                if (full == null || full == code && !(type == "script" && KnownScript(code))) return null;
            }
            return result;
        }
    }
}
