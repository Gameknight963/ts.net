using System;
using System.Collections.Generic;
static class Program {
 static string Keys(TsObject source){var keys=(JsArray)source.OwnKeys();var names=new List<string>();for(uint i=0;i<keys.Length();i++)names.Add((string)keys.ElementAt(i));return string.Join(",",names);}
 static void Print(params object[] args){Console.WriteLine(string.Join(" ",Array.ConvertAll(args,Js.Text)));}
 static void Main(){
  var record=new JsDynamicObject();foreach(string key in new[]{"a","b","c","d","e","f","g"})record.DefineOwn(key,key);
  Print("delete",record.DeleteOwn("b"),record.DeleteOwn("f"),record.DeleteOwn("missing"));record.DefineOwn("b",2d);record.DefineOwn("f",6d);Print("order",Keys(record));
  record.DeleteOwn("e");record.DefineOwn("h",8d);record.DeleteOwn("a");record.DefineOwn("a",1d);Print("promote",Keys(record));
  var clone=(TsObject)record.CloneForSpread();clone.DeleteOwn("g");clone.DefineOwn("g",7d);Print("clone",Keys(clone),Keys(record));
  var accessor=new JsFunction((FunctionBody)(_=>throw new Exception("Unexpected getter")),null,"",0);record.DefineAccessor("accessor",accessor,1);Print("accessor",record.DeleteOwn("accessor"),record.HasOwn("accessor"));
  foreach(JsArray array in new JsArray[]{new JsArray(new object[]{1d,2d,3d}),new NumberArray(0),new TaggedArray(new object[]{1d,2d,3d}),new ReversibleArray(new object[]{1d,2d,3d})}){
   if(array is NumberArray){array.Append(1d);array.Append(2d);array.Append(3d);}
   if(array is ReversibleArray)array.Reverse();
   var prototype=new JsDynamicObject();prototype.DefineOwn("1",99d);
   object ctor=Js.BuiltinNamespace("Object");Js.CallMethod(Js.Get(ctor,"setPrototypeOf"),ctor,new object[]{array,prototype});
   Print("element",array.DeleteOwn("1"),array.Length(),array.HasOwn("1"),array.ElementAt(1),Keys(array));
   array.SetAt(1,8d);Print("rewrite",array.ElementAt(1),Keys(array),array.DeleteOwn("length"));
   array.ElementAt(0);array.DeleteOwn("1");Print("materialized",array.ElementAt(1),array.HasOwn("1"));
   array.Set("4000000000",7d);array.DeleteOwn("4000000000");Print("sparse",array.Length(),array.HasOwn("4000000000"));
  }
 }
}
