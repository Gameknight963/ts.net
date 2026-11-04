using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

public static class Writer
{
    // Status codes keep exceptions inside the managed/native boundary.
    [UnmanagedCallersOnly(EntryPoint = "tsnet_write_test_assembly")]
    public static unsafe int Write(byte* path)
    {
        if (path == null) return 1;
        try
        {
            WriteAssembly(Marshal.PtrToStringUTF8((nint)path)!);
            return 0;
        }
        catch { return 2; }
    }

    private static void WriteAssembly(string path)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("Generated.dll"),
            metadata.GetOrAddGuid(new Guid("34DA44E8-927E-471B-AE05-107204FB684B")), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("Generated"), new Version(1, 0, 0, 0),
            default, default, 0, AssemblyHashAlgorithm.None);
        var token = metadata.GetOrAddBlob(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a });
        var runtime = metadata.AddAssemblyReference(metadata.GetOrAddString("System.Runtime"),
            new Version(10, 0, 0, 0), default, token, 0, default);
        var console = metadata.AddAssemblyReference(metadata.GetOrAddString("System.Console"),
            new Version(10, 0, 0, 0), default, token, 0, default);
        var objectType = metadata.AddTypeReference(runtime, metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
        var consoleType = metadata.AddTypeReference(console, metadata.GetOrAddString("System"), metadata.GetOrAddString("Console"));
        var printSignature = new BlobBuilder();
        new BlobEncoder(printSignature).MethodSignature().Parameters(1,
            result => result.Void(), parameters => parameters.AddParameter().Type().Int32());
        var print = metadata.AddMemberReference(consoleType, metadata.GetOrAddString("WriteLine"), metadata.GetOrAddBlob(printSignature));
        var mainSignature = new BlobBuilder();
        new BlobEncoder(mainSignature).MethodSignature().Parameters(0, result => result.Void(), _ => { });
        var code = new BlobBuilder();
        var il = new InstructionEncoder(code);
        il.LoadConstantI4(42);
        il.Call(print);
        il.OpCode(ILOpCode.Ret);
        var bodies = new BlobBuilder();
        int offset = new MethodBodyStreamEncoder(bodies).AddMethodBody(il);
        var main = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
            MethodImplAttributes.IL, metadata.GetOrAddString("Main"), metadata.GetOrAddBlob(mainSignature),
            offset, MetadataTokens.ParameterHandle(1));
        metadata.AddTypeDefinition(0, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), main);
        metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
            default, metadata.GetOrAddString("Program"), objectType, MetadataTokens.FieldDefinitionHandle(1), main);
        var pe = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage),
            new MetadataRootBuilder(metadata), bodies, entryPoint: main, flags: CorFlags.ILOnly);
        var image = new BlobBuilder();
        pe.Serialize(image);
        using var output = File.Create(path);
        image.WriteContentTo(output);
    }
}
