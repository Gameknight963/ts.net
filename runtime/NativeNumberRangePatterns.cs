using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static partial class Js
{
    sealed record NumberRangePatterns(string Range,string Approximately);
    static class NativeNumberRangePatterns
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(IntPtr package,[MarshalAs(UnmanagedType.LPUTF8Str)] string locale,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Get(Handle parent,[MarshalAs(UnmanagedType.LPUTF8Str)] string key,IntPtr fill,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Text(Handle parent,[MarshalAs(UnmanagedType.LPUTF8Str)] string key,out int length,ref int error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Close(IntPtr resource);
        sealed record Api(IcuModule Module,Open Open,Get Get,Text Text,Close Close);
        sealed class Handle(IcuModule module,Close close):SafeHandle(IntPtr.Zero,true)
        {
            public override bool IsInvalid=>handle==IntPtr.Zero;
            internal void Initialize(IntPtr value)=>SetHandle(value);
            protected override bool ReleaseHandle(){try{using var thread=module.Enter();close(handle);return true;}catch{return false;}}
        }
        static readonly Lazy<Api> Backend=new(()=>PlatformIcu.Load(Bind));
        static readonly Dictionary<(string,string),NumberRangePatterns> Cache=new();
        static Api Bind(IcuModule module)
        {
            if(!module.Function<Open>("ures_open",out var open) || !module.Function<Get>("ures_getByKey",out var get) ||
                !module.Function<Text>("ures_getStringByKey",out var text) || !module.Function<Close>("ures_close",out var close))return null;
            return new(module,open,get,text,close);
        }
        static void Check(int error){if(error>0)throw new InvalidOperationException("Native number range data failed: ICU status "+error);}
        static (string Range,string Approximately) Read(Api api,string locale,string system)
        {
            using var thread=api.Module.Enter();var resources=new List<Handle>();
            try {
                int error=0;var root=new Handle(api.Module,api.Close);resources.Add(root);root.Initialize(api.Open(IntPtr.Zero,locale,ref error));
                if(error==2)return default;Check(error);if(root.IsInvalid)throw new InvalidOperationException("Native range resources were not opened");
                var parent=root;
                foreach(string key in new[]{"NumberElements",system,"miscPatterns"}) {
                    var child=new Handle(api.Module,api.Close);resources.Add(child);child.Initialize(api.Get(parent,key,IntPtr.Zero,ref error));
                    if(error==2)return default;Check(error);if(child.IsInvalid)throw new InvalidOperationException("Native range resource is invalid");parent=child;
                }
                string Pattern(string key) {
                    int status=0;IntPtr chars=api.Text(parent,key,out int length,ref status);if(status==2)return null;Check(status);
                    if(chars==IntPtr.Zero || length<0 || length>256)throw new InvalidOperationException("Invalid native number range pattern");
                    return Marshal.PtrToStringUni(chars,length);
                }
                return (Pattern("range"),Pattern("approximately"));
            }finally{for(int i=resources.Count-1;i>=0;i--)resources[i].Dispose();}
        }
        internal static NumberRangePatterns For(string locale,string system)
        {
            lock(Cache) {
                var key=(locale,system);if(Cache.TryGetValue(key,out var result))return result;
                var api=Backend.Value ?? throw new NotSupportedException("Regional number ranges require system ICU");
                string range=null,approximately=null,name=locale.Replace('-','_');if(name.Length==0)name="root";
                while(true) {
                    foreach(string numbers in system=="latn" ? new[]{"latn"} : new[]{system,"latn"}) {
                        var found=Read(api,name,numbers);range??=found.Range;approximately??=found.Approximately;
                        if(range!=null && approximately!=null)break;
                    }
                    if(range!=null && approximately!=null || name=="root")break;
                    int parent=name.LastIndexOf('_');name=parent<0 ? "root" : name[..parent];
                }
                static bool Once(string text,string token)=>text!=null && text.IndexOf(token,StringComparison.Ordinal)>=0 && text.IndexOf(token,StringComparison.Ordinal)==text.LastIndexOf(token,StringComparison.Ordinal);
                if(!Once(range,"{0}") || !Once(range,"{1}") || !Once(approximately,"{0}") || approximately.Contains("{1}",StringComparison.Ordinal))throw new NotSupportedException("System ICU did not supply valid number range patterns");
                result=new(range,approximately);if(Cache.Count>=64)Cache.Clear();Cache.Add(key,result);return result;
            }
        }
    }
}
