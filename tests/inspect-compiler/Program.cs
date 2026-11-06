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
