using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static string[] Check(string path,params string[] prefixes){
 using var stream=File.OpenRead(path);using var pe=new PEReader(stream);var metadata=pe.GetMetadataReader();
 var names=metadata.MethodDefinitions.Select(handle=>metadata.GetString(metadata.GetMethodDefinition(handle).Name)).ToArray();
 foreach(string prefix in prefixes)if(!names.Any(name=>name.StartsWith(prefix,StringComparison.Ordinal)))throw new Exception("Missing readable method name: "+prefix);
 if(names.Any(name=>name.Contains('\0')))throw new Exception("Null character in metadata name");
 return names;
}
var names=Check(args[0],"buildError_f","buildError_a","arrowName_f","innerName_f","read_value_f","\u96ea_f","\U00010400_f","cafe\u0301_f");
if(names.Count(name=>name.StartsWith("sameName_f",StringComparison.Ordinal))!=2)throw new Exception("Shadowed functions lost distinct names");
Check(args[1],"asyncName_resume_f","generatorName_resume_f","Sample_constructor_f","Sample_get_current_f","Sample_read_f");
Console.WriteLine("PASS readable function, class, async, generator, adapter, and Unicode metadata names");
