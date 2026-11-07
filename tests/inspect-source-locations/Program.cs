using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

int methods=0,points=0;
foreach(string path in args) {
 using var stream=File.OpenRead(path);using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();
 var entry=pe.ReadDebugDirectory().Single(value=>value.Type==DebugDirectoryEntryType.EmbeddedPortablePdb);
 using var provider=pe.ReadEmbeddedPortablePdbDebugDirectoryData(entry);var pdb=provider.GetMetadataReader();
 if(pdb.MethodDebugInformation.Count!=metadata.MethodDefinitions.Count)throw new Exception("PDB methods do not align with assembly methods");
 bool unicodeFile=false,unicodeColumn=false,optimizedReturn=false;
 foreach(var handle in metadata.MethodDefinitions) {
  var method=metadata.GetMethodDefinition(handle);
  if((method.ImplAttributes&(MethodImplAttributes.NoInlining|MethodImplAttributes.NoOptimization))!=0)throw new Exception("Source locations disabled optimization");
  if(method.RelativeVirtualAddress==0)continue;
  int size=pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!.Length,previous=-1;
  foreach(var point in pdb.GetMethodDebugInformation(handle).GetSequencePoints()) {
   if(point.Offset<=previous || point.Offset>=size)throw new Exception("Sequence point is not at a valid IL offset");
   previous=point.Offset;if(point.IsHidden)continue;
   string source=pdb.GetString(pdb.GetDocument(point.Document).Name);
   if(!source.EndsWith(".ts") || point.StartLine<1 || point.StartColumn<1 || point.EndColumn<=point.StartColumn)throw new Exception("Invalid source position");
   if(source.EndsWith("stack-location-unicode.ts")) {
    unicodeFile=true;
    if(point.StartLine==1 && point.StartColumn==File.ReadLines(source).First().IndexOf("failure",StringComparison.Ordinal)+1)unicodeColumn=true;
    if(point.StartLine==6)optimizedReturn=true;
   }
   points++;
  }
  methods++;
 }
 if(unicodeFile && (!unicodeColumn || !optimizedReturn))throw new Exception("Missing UTF-16 column or optimized return location");
 if(pdb.Documents.Count==0)throw new Exception("PDB has no source documents");
}
if(points==0)throw new Exception("PDB has no sequence points");
Console.WriteLine($"PASS embedded TypeScript locations: {methods} methods, {points} source points; optimization remains enabled");
