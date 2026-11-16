using Tsnet.Compiler;
using System.Runtime.CompilerServices;
var store = new Store<Node>();
for (int i=0;i<10000;i++) {
    int id = store.add(new Node {left=i,kind=NodeKind.Number});
    if (id!=i+1) throw new Exception("Changed one-based ID");
}
for (int i=1;i<=10000;i++) if(store.get(i).left!=i-1) throw new Exception("Lost record through growth");
Node copy=store.get(1);copy.left=99;
if(store.get(1).left!=0) throw new Exception("Reads must return value copies");
store.at_mut(1).left=88;
if(store.get(1).left!=88) throw new Exception("Record update failed");
string directory=Path.Combine(Path.GetTempPath(),"tsnet-writer-test-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string path=Path.Combine(directory,"preserved.dll");
try {
    File.WriteAllText(path,"sentinel");
    var plan=Tsnet.Compiler.AssemblyPlan.create();
    plan.type("Invalid",999,1);
    bool failed=false;
    try {plan.finish(999,path);} catch(Exception) {failed=true;}
    if(!failed || File.ReadAllText(path)!="sentinel") throw new Exception("Failed emission replaced output");
    if(Directory.GetFiles(directory).Length!=1) throw new Exception("Failed emission left temporary output");
} finally {File.Delete(path);Directory.Delete(directory);}
Console.WriteLine("PASS compiler store growth, value copies, mutation, and writer output preservation");

using var diagnostic = new MemoryStream();
var output = new Tsnet.Compiler.Output(diagnostic);
foreach (byte unit in System.Text.Encoding.UTF8.GetBytes("\u96EA")) output.put(unit);
output.put('('); output.integer(12); output.bytes("): error");
if (System.Text.Encoding.UTF8.GetString(diagnostic.ToArray()) != "\u96EA(12): error") throw new Exception("Diagnostic changed UTF-8 bytes");
Console.WriteLine("PASS UTF-8 compiler diagnostics");

static void ReaderFailure(Action read, string message) {
    try {read();} catch(CompilationException error) when(error.Message == message) {return;}
    throw new Exception("Frontend reader accepted invalid input");
}
var reader = IrReader.create(new byte[] {0xFF, 0x78, 0x56, 0x34, 0x12, 0xFF, 0xFF, 0xFF, 0x7F});
if (reader.ReadByte() != 0xFF || reader.integer() != 0x12345678 || reader.integer() != int.MaxValue || !reader.AtEnd)
    throw new Exception("Frontend reader changed little-endian integers or unaligned reads");
ReaderFailure(() => reader.ReadByte(), "truncated frontend IR");
for (int size = 0; size < 4; size++) {
    var truncated = IrReader.create(new byte[size]);
    ReaderFailure(() => truncated.integer(), "truncated frontend IR");
}
ReaderFailure(() => IrReader.create(new byte[] {0, 0, 0, 0x80}).integer(), "invalid frontend IR integer");
byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("a\u96EA\U0001F600\0z");
byte[] payload = new byte[4 + utf8.Length];
System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload, utf8.Length);
utf8.CopyTo(payload, 4);
var parsed = new Compiler {source = new Source(), text_store = new Store<TextRange>()};
var textReader = IrReader.create(payload);
textReader.text(parsed);
if (!textReader.AtEnd || parsed.source.length() != utf8.Length || parsed.text_store.get(1).length != utf8.Length)
    throw new Exception("Frontend reader changed UTF-8 text length");
for (int i = 0; i < utf8.Length; i++) if (parsed.source.peek(i) != utf8[i]) throw new Exception("Frontend reader changed text bytes");
ReaderFailure(() => IrReader.create(new byte[] {2, 0, 0, 0, 1}).text(parsed), "truncated frontend IR");
ReaderFailure(() => IrReader.create(new byte[] {0xFF, 0xFF, 0xFF, 0x7F}).text(parsed), "truncated frontend IR");
var empty = IrReader.create(new byte[4]);
empty.text(parsed);
if (!empty.AtEnd || parsed.text_store.get(2).length != 0) throw new Exception("Frontend reader changed empty text");
Console.WriteLine("PASS frontend integer boundaries, truncated input, and exact UTF-8 text bytes");

byte[] largeInput = new byte[4 * 1024 * 1024];
GC.KeepAlive(IrReader.create(largeInput));
long allocationStart = GC.GetAllocatedBytesForCurrentThread();
var largeReader = IrReader.create(largeInput);
long readerBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
GC.KeepAlive(largeReader);
if (readerBytes > 1024) throw new Exception("Frontend reader copied the large input buffer");
Console.WriteLine($"PASS frontend reader allocation limit ({readerBytes} bytes for 4 MiB input)");
