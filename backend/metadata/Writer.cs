using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

// This boundary accepts assembly concepts, not TypeScript syntax or types.
// IDs belong to the input plan; metadata tokens are assigned only here.
public static class NativeWriter
{
    [UnmanagedCallersOnly(EntryPoint = "tsnet_emit_assembly")]
    public static unsafe int Emit(byte* data, int length, byte* path, byte* error, int capacity)
    {
        try
        {
            if (data == null || length < 4 || path == null || capacity < 0) throw new ArgumentException("Invalid writer arguments");
            var plan = new AssemblyPlan(new BinaryReader(new MemoryStream(new ReadOnlySpan<byte>(data, length).ToArray())));
            var image = plan.Build();
            string destination = Marshal.PtrToStringUTF8((nint)path)!;
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporary, image); File.Move(temporary, destination, overwrite: true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return 0;
        }
        catch (Exception exception)
        {
            if (error != null && capacity > 0)
            {
                byte[] message = Encoding.UTF8.GetBytes(exception.Message);
                int count = Math.Min(message.Length, capacity - 1);
                message.AsSpan(0, count).CopyTo(new Span<byte>(error, count));
                error[count] = 0;
            }
            return 1;
        }
    }
}

sealed class AssemblyPlan
{
    sealed record ExternalType(int Id, string Assembly, string Namespace, string Name);
    sealed class TypeRow(int id, int attributes, string name, int parent)
    {
        public int Id = id, Attributes = attributes, Parent = parent;
        public string Name = name;
        public List<int> Interfaces = [];
        public List<FieldRow> Fields = [];
        public List<MethodRow> Methods = [];
    }
    sealed record FieldRow(int Id, int Attributes, int Type, string Name);
    sealed record Instruction(int Code, int Operand, double Number = 0, string? Text = null);
    sealed class MethodRow(int id, int owner, int attributes, int result, int[] parameters, string name)
    {
        public int Id = id, Owner = owner, Attributes = attributes, Result = result;
        public int[] Parameters = parameters;
        public string Name = name;
        public List<int> Locals = [];
        public List<Instruction> Instructions = [];
    }
    readonly List<ExternalType> references = [];
    readonly List<TypeRow> types = [];
    readonly List<MethodRow> members = [];
    readonly List<(int Id, int Method, int Type)> specifications = [];
    readonly Dictionary<int, TypeRow> typeRows = [];
    readonly Dictionary<int, MethodRow> methodRows = [];
    readonly Dictionary<int, EntityHandle> handles = [];
    readonly MetadataBuilder metadata = new();
    TypeReferenceHandle nullableType;
    readonly int entry;
    readonly string name;
    static readonly UTF8Encoding Utf8 = new(false, true);

    internal static string Text(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length < 0 || length > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid string length");
        byte[] bytes = reader.ReadBytes(length);
        // The frontend uses WTF-8 so JavaScript's lone UTF-16 surrogates survive
        // the native boundary. Decode those triples, validating all other UTF-8.
        StringBuilder? result = null;
        int start = 0;
        for (int i = 0; i + 2 < bytes.Length; i++)
        {
            if (bytes[i] != 0xed || bytes[i + 1] < 0xa0 || bytes[i + 1] > 0xbf || (bytes[i + 2] & 0xc0) != 0x80) continue;
            result ??= new StringBuilder();
            result.Append(Utf8.GetString(bytes, start, i - start));
            result.Append((char)(((bytes[i] & 15) << 12) | ((bytes[i + 1] & 63) << 6) | (bytes[i + 2] & 63)));
            i += 2;start = i + 1;
        }
        if (result == null) return Utf8.GetString(bytes);
        result.Append(Utf8.GetString(bytes, start, bytes.Length - start));
        return result.ToString();
    }
    static int[] Parameters(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > 65535) throw new InvalidDataException("Invalid parameter count");
        var result = new int[count];
        for (int i = 0; i < count; i++) result[i] = reader.ReadInt32();
        return result;
    }
    public AssemblyPlan(BinaryReader reader)
    {
        if (reader.ReadInt32() != 0x314c4954) throw new InvalidDataException("Unsupported IL plan version");
        name = Text(reader);
        while (true)
        {
            int command = reader.ReadInt32();
            int id = reader.ReadInt32();
            switch (command)
            {
                case 1: references.Add(new(id, Text(reader), Text(reader), Text(reader))); break;
                case 2:
                    var type = new TypeRow(id, reader.ReadInt32(), Text(reader), reader.ReadInt32());
                    types.Add(type); typeRows.Add(id, type); break;
                case 3: typeRows[id].Interfaces.Add(reader.ReadInt32()); break;
                case 4:
                    int owner = reader.ReadInt32();
                    typeRows[owner].Fields.Add(new(id, reader.ReadInt32(), reader.ReadInt32(), Text(reader))); break;
                case 5: case 6:
                    var method = new MethodRow(id, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), Parameters(reader), Text(reader));
                    methodRows.Add(id, method);
                    if (command == 5) typeRows[method.Owner].Methods.Add(method); else members.Add(method);
                    break;
                case 7: methodRows[id].Locals.Add(reader.ReadInt32()); break;
                case 8: methodRows[id].Instructions.Add(new(reader.ReadInt32(), reader.ReadInt32())); break;
                case 9: methodRows[id].Instructions.Add(new(0x23, 0, reader.ReadDouble())); break;
                case 10: methodRows[id].Instructions.Add(new(0x72, 0, Text: Text(reader))); break;
                case 11:
                    entry = id;
                    if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing IL plan data");
                    return;
                case 12: specifications.Add((id, reader.ReadInt32(), reader.ReadInt32())); break;
                default: throw new InvalidDataException("Unknown IL plan command");
            }
        }
    }

    EntityHandle Handle(int id) => id == 0 ? default : handles[id];
    void EncodeType(SignatureTypeEncoder encoder, int type)
    {
        switch (type)
        {
            case -2: encoder.Boolean(); break;
            case -3: encoder.Double(); break;
            case -4: encoder.String(); break;
            case -5: encoder.Object(); break;
            case -6: encoder.Int32(); break;
            case -7: encoder.SZArray().Object(); break;
            case -8: encoder.GenericMethodTypeParameter(0); break;
            case -9: encoder.IntPtr(); break;
            case -10: encoder.GenericInstantiation(nullableType, 1, isValueType: true).AddArgument().Double(); break;
            case -11: encoder.GenericInstantiation(nullableType, 1, isValueType: true).AddArgument().Boolean(); break;
            case -12: encoder.SZArray().Double(); break;
            case -13: encoder.SZArray().Byte(); break;
            default: encoder.Type(Handle(type), isValueType: false); break;
        }
    }
    BlobHandle Signature(MethodRow method, bool external)
    {
        var signature = new BlobBuilder();
        bool instance = external ? (method.Attributes & 1) != 0 : (method.Attributes & (int)MethodAttributes.Static) == 0;
        int genericCount = external && (method.Attributes & 2) != 0 ? 1 : 0;
        new BlobEncoder(signature).MethodSignature(genericParameterCount: genericCount, isInstanceMethod: instance).Parameters(method.Parameters.Length,
            result => { if (method.Result == -1) result.Void(); else EncodeType(result.Type(), method.Result); },
            parameters => { foreach (int parameter in method.Parameters) EncodeType(parameters.AddParameter().Type(), parameter); });
        return metadata.GetOrAddBlob(signature);
    }
    int MaxStack(MethodRow method)
    {
        var instructions = method.Instructions;
        var labels = new Dictionary<int, int>();
        for (int i = 0; i < instructions.Count; i++)
            if (instructions[i].Code == -1) labels.Add(instructions[i].Operand, i);
        var depths = new Dictionary<int, int>();
        var pending = new Queue<int>();
        int maximum = 0;
        void Visit(int index, int depth)
        {
            if (index >= instructions.Count) throw new InvalidDataException($"Method falls through: {method.Name}");
            if (depths.TryGetValue(index, out int previous))
            {
                if (previous != depth) throw new InvalidDataException($"Inconsistent stack at branch in {method.Name}");
                return;
            }
            depths.Add(index, depth); pending.Enqueue(index);
            maximum = Math.Max(maximum, depth);
        }
        Visit(0, 0);
        while (pending.TryDequeue(out int index))
        {
            var instruction = instructions[index];
            int pop = 0, push = 0;
            switch (instruction.Code)
            {
                case -1: case 0x00: case 0x38: break;
                case 0x14: case 0x16: case 0x20: case 0x23: case 0x72:
                case 0x7e: case 0xfe06: case 0xfe09: case 0xfe0c: push = 1; break;
                case 0x25: pop = 1; push = 2; break;
                case 0x26: case 0x39: case 0x3a: case 0x80: case 0xfe0b: case 0xfe0e: pop = 1; break;
                case 0x58: case 0x59: case 0x5a: case 0x5b: case 0x5d: case 0x5e:
                case 0x5f: case 0x60: case 0x61: case 0x62: case 0x63: case 0x64:
                case 0xfe01: case 0xfe02: case 0xfe03: case 0xfe04: case 0xfe05: pop = 2; push = 1; break;
                case 0x91: case 0x99: case 0x9a: pop = 2; push = 1; break;
                case 0x65: case 0x66: case 0x69: case 0x6c: case 0x74: case 0x75: case 0x76: case 0x7b:
                case 0x8c: case 0x8d: case 0xa5: pop = 1; push = 1; break;
                case 0x7d: pop = 2; break;
                case 0xa1: case 0xa2: pop = 3; break;
                case 0x28: case 0x6f: case 0x73:
                    int target = instruction.Operand;
                    if (!methodRows.ContainsKey(target)) target = specifications.First(s => s.Id == target).Method;
                    var called = methodRows[target];
                    bool instance = members.Contains(called) ? (called.Attributes & 1) != 0 :
                        (called.Attributes & (int)MethodAttributes.Static) == 0;
                    pop = called.Parameters.Length + (instance && instruction.Code != 0x73 ? 1 : 0);
                    push = instruction.Code == 0x73 || called.Result != -1 ? 1 : 0;
                    break;
                case 0x2a: pop = method.Result == -1 ? 0 : 1; break;
                default: throw new InvalidDataException($"Missing stack accounting for opcode {instruction.Code:x}");
            }
            int depth = depths[index];
            if (depth < pop) throw new InvalidDataException($"Stack underflow in {method.Name}");
            depth = depth - pop + push;
            maximum = Math.Max(maximum, depth);
            if (maximum > ushort.MaxValue) throw new InvalidDataException("Method stack exceeds CLR limit");
            if (instruction.Code == 0x2a)
            {
                if (depth != 0) throw new InvalidDataException($"Nonempty return stack in {method.Name}");
                continue;
            }
            if (instruction.Code is 0x38 or 0x39 or 0x3a) Visit(labels[instruction.Operand], depth);
            if (instruction.Code != 0x38) Visit(index + 1, depth);
        }
        return maximum;
    }
    int Body(MethodRow method, MethodBodyStreamEncoder bodies)
    {
        if ((method.Attributes & (int)MethodAttributes.Abstract) != 0) return -1;
        var code = new BlobBuilder();
        var flow = new ControlFlowBuilder();
        var il = new InstructionEncoder(code, flow);
        var labels = new Dictionary<int, LabelHandle>();
        LabelHandle Label(int id) { if (!labels.TryGetValue(id, out var label)) labels.Add(id, label = il.DefineLabel()); return label; }
        foreach (var instruction in method.Instructions)
        {
            int operand = instruction.Operand;
            switch (instruction.Code)
            {
                case -1: il.MarkLabel(Label(operand)); break;
                case 0x20: il.LoadConstantI4(operand); break;
                case 0x23: il.LoadConstantR8(instruction.Number); break;
                case 0x72: il.LoadString(metadata.GetOrAddUserString(instruction.Text!)); break;
                case 0xfe09: il.LoadArgument(operand); break;
                case 0xfe0b: il.StoreArgument(operand); break;
                case 0xfe0c: il.LoadLocal(operand); break;
                case 0xfe0e: il.StoreLocal(operand); break;
                case 0x38: case 0x39: case 0x3a: il.Branch((ILOpCode)instruction.Code, Label(operand)); break;
                case 0x28: case 0x6f: case 0x73: case 0x74: case 0x75: case 0x7b: case 0x7d: case 0x7e: case 0x80: case 0x8c: case 0x8d: case 0xa5: case 0xfe06:
                    if (operand == 0) throw new InvalidDataException($"Missing IL operand in {method.Name}, opcode {instruction.Code:x}");
                    il.OpCode((ILOpCode)instruction.Code); il.Token(Handle(operand)); break;
                default: il.OpCode((ILOpCode)instruction.Code); break;
            }
        }
        StandaloneSignatureHandle locals = default;
        if (method.Locals.Count != 0)
        {
            var signature = new BlobBuilder();
            var encoder = new BlobEncoder(signature).LocalVariableSignature(method.Locals.Count);
            foreach (int local in method.Locals) EncodeType(encoder.AddVariable().Type(), local);
            locals = metadata.AddStandaloneSignature(metadata.GetOrAddBlob(signature));
        }
        return bodies.AddMethodBody(il, maxStack: MaxStack(method), localVariablesSignature: locals, attributes: MethodBodyAttributes.InitLocals);
    }
    public byte[] Build()
    {
        metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(name), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.None);
        var assemblies = new Dictionary<string, AssemblyReferenceHandle>();
        foreach (var reference in references)
        {
            if (!assemblies.TryGetValue(reference.Assembly, out var assembly))
            {
                bool framework = reference.Assembly.StartsWith("System.", StringComparison.Ordinal);
                var key = framework ? metadata.GetOrAddBlob(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }) : default;
                assembly = metadata.AddAssemblyReference(metadata.GetOrAddString(reference.Assembly),
                    framework ? new Version(10, 0, 0, 0) : new Version(1, 0, 0, 0), default, key, 0, default);
                assemblies.Add(reference.Assembly, assembly);
            }
            handles.Add(reference.Id, metadata.AddTypeReference(assembly, metadata.GetOrAddString(reference.Namespace), metadata.GetOrAddString(reference.Name)));
        }
        metadata.AddTypeDefinition(0, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        nullableType = metadata.AddTypeReference(assemblies["System.Runtime"], metadata.GetOrAddString("System"), metadata.GetOrAddString("Nullable`1"));
        int typeIndex = 2, fieldIndex = 1, methodIndex = 1;
        foreach (var type in types)
        {
            handles.Add(type.Id, MetadataTokens.TypeDefinitionHandle(typeIndex++));
            foreach (var field in type.Fields) handles.Add(field.Id, MetadataTokens.FieldDefinitionHandle(fieldIndex++));
            foreach (var method in type.Methods) handles.Add(method.Id, MetadataTokens.MethodDefinitionHandle(methodIndex++));
        }
        foreach (var member in members) handles.Add(member.Id, metadata.AddMemberReference(Handle(member.Owner), metadata.GetOrAddString(member.Name), Signature(member, true)));
        foreach (var specification in specifications)
        {
            var signature = new BlobBuilder();
            EncodeType(new BlobEncoder(signature).MethodSpecificationSignature(1).AddArgument(), specification.Type);
            handles.Add(specification.Id, metadata.AddMethodSpecification(Handle(specification.Method), metadata.GetOrAddBlob(signature)));
        }
        var il = new BlobBuilder();
        var bodies = new MethodBodyStreamEncoder(il);
        fieldIndex = methodIndex = 1;
        foreach (var type in types)
        {
            metadata.AddTypeDefinition((TypeAttributes)type.Attributes, default, metadata.GetOrAddString(type.Name), Handle(type.Parent),
                MetadataTokens.FieldDefinitionHandle(fieldIndex), MetadataTokens.MethodDefinitionHandle(methodIndex));
            foreach (int implemented in type.Interfaces) metadata.AddInterfaceImplementation((TypeDefinitionHandle)Handle(type.Id), Handle(implemented));
            foreach (var field in type.Fields)
            {
                var signature = new BlobBuilder();
                EncodeType(new BlobEncoder(signature).FieldSignature(), field.Type);
                metadata.AddFieldDefinition((FieldAttributes)field.Attributes, metadata.GetOrAddString(field.Name), metadata.GetOrAddBlob(signature));
                fieldIndex++;
            }
            foreach (var method in type.Methods)
            {
                metadata.AddMethodDefinition((MethodAttributes)method.Attributes, MethodImplAttributes.IL, metadata.GetOrAddString(method.Name),
                    Signature(method, false), Body(method, bodies), MetadataTokens.ParameterHandle(1));
                methodIndex++;
            }
        }
        var pe = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage), new MetadataRootBuilder(metadata), il,
            entryPoint: (MethodDefinitionHandle)Handle(entry), flags: CorFlags.ILOnly);
        var result = new BlobBuilder();
        pe.Serialize(result);
        return result.ToArray();
    }
}
