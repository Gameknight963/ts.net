using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static partial class Js
{
    // .NET 10's portable compression ABI exposes the decoder's consumed-input
    // count and end marker, which its public Stream wrappers do not expose.
    sealed class NativeInflate : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct State {internal IntPtr NextIn,NextOut,Message,InternalState;internal uint AvailIn,AvailOut;}
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int Initialize(Context context,int windowBits);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int Inflate(Context context,int flush);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate int End(IntPtr context);
        sealed class Api
        {
            internal readonly Initialize Init;
            internal readonly Inflate Step;
            internal readonly End Finish;
            internal Api()
            {
                if (System.Environment.Version.Major != 10) throw new NotSupportedException("Native deflate decoding currently requires the .NET 10 compression ABI");
                string file = OperatingSystem.IsWindows() ? "System.IO.Compression.Native.dll" : OperatingSystem.IsMacOS() ? "libSystem.IO.Compression.Native.dylib" : "libSystem.IO.Compression.Native.so";
                string path = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(),file);
                if (!NativeLibrary.TryLoad(path,out var library) && !NativeLibrary.TryLoad("System.IO.Compression.Native",typeof(GZipStream).Assembly,DllImportSearchPath.SafeDirectories,out library))
                    throw new NotSupportedException("The .NET native compression library is unavailable");
                try {
                    Init = Marshal.GetDelegateForFunctionPointer<Initialize>(NativeLibrary.GetExport(library,"CompressionNative_InflateInit2_"));
                    Step = Marshal.GetDelegateForFunctionPointer<Inflate>(NativeLibrary.GetExport(library,"CompressionNative_Inflate"));
                    Finish = Marshal.GetDelegateForFunctionPointer<End>(NativeLibrary.GetExport(library,"CompressionNative_InflateEnd"));
                }
                catch {NativeLibrary.Free(library);throw;}
                // The library remains loaded for the delegates' process lifetime.
            }
        }
        sealed class Context : SafeHandleZeroOrMinusOneIsInvalid
        {
            readonly Api api;
            bool initialized;
            internal Context(Api api,int bits) : base(true)
            {
                this.api = api;SetHandle(Marshal.AllocHGlobal(Marshal.SizeOf<State>()));Marshal.StructureToPtr(default(State),handle,false);
                try {
                    int result = api.Init(this,bits);
                    if (result != 0) throw new InvalidOperationException("Cannot initialize decompression context");
                    initialized = true;
                }
                catch {Dispose();throw;}
            }
            protected override bool ReleaseHandle()
            {
                try {return !initialized || api.Finish(handle) == 0;}
                catch {return false;}
                finally {Marshal.FreeHGlobal(handle);}
            }
        }
        static readonly Lazy<Api> Backend = new(() => new Api());
        readonly Api api;
        readonly Context context;
        internal NativeInflate(string format)
        {
            api = Backend.Value;context = new Context(api,format == "gzip" ? 31 : format == "deflate" ? 15 : -15);
        }
        internal int Decode(byte[] input,int offset,int count,byte[] output,out int consumed,out int written)
        {
            bool retained = false;GCHandle sourcePin = default,targetPin = default;
            try {
                context.DangerousAddRef(ref retained);
                if (count != 0) sourcePin = GCHandle.Alloc(input,GCHandleType.Pinned);
                targetPin = GCHandle.Alloc(output,GCHandleType.Pinned);
                IntPtr address = context.DangerousGetHandle();var state = Marshal.PtrToStructure<State>(address);
                state.NextIn = count == 0 ? IntPtr.Zero : IntPtr.Add(sourcePin.AddrOfPinnedObject(),offset);
                state.NextOut = targetPin.AddrOfPinnedObject();state.AvailIn = (uint)count;state.AvailOut = (uint)output.Length;
                Marshal.StructureToPtr(state,address,false);
                int status = api.Step(context,0);state = Marshal.PtrToStructure<State>(address);
                if (state.AvailIn > count || state.AvailOut > output.Length) throw new InvalidOperationException("Invalid native decoder buffer accounting");
                consumed = count-(int)state.AvailIn;written = output.Length-(int)state.AvailOut;
                state.NextIn = state.NextOut = IntPtr.Zero;state.AvailIn = state.AvailOut = 0;Marshal.StructureToPtr(state,address,false);
                return status;
            }
            finally {
                if (sourcePin.IsAllocated) sourcePin.Free();if (targetPin.IsAllocated) targetPin.Free();
                if (retained) context.DangerousRelease();
            }
        }
        public void Dispose() => context.Dispose();
    }
}
