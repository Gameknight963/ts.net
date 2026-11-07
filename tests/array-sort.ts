const numbers=[10,2,1,20];console.log(numbers.sort()===numbers,numbers.join("|"));numbers.sort((a,b)=>a-b);console.log(numbers.join("|"));
const values:any[]=[3,undefined,1,,2,undefined,,];values.sort();console.log(values.length,values.join("|"),Object.keys(values).join("|"));
const stable=[{key:2,id:"a"},{key:1,id:"b"},{key:2,id:"c"},{key:1,id:"d"}];stable.sort((a,b)=>a.key-b.key);console.log(stable.map(x=>x.id).join(""));
const source:any={0:"c",2:"a",length:4};console.log((Array.prototype.sort as any).call(source)===source,JSON.stringify(source));
const inherited:any=Object.create({1:"b"});inherited[0]="c";inherited[2]="a";inherited.length=4;(Array.prototype.sort as any).call(inherited);console.log(inherited[0],inherited[1],inherited[2],Object.keys(inherited).join("|"));
const empty:number[]=[];console.log(empty.sort()===empty,empty.length);
const equal=[3,2,1];equal.sort(()=>NaN);console.log(equal.join("|"));
const thrown=[3,1,2];try{thrown.sort(()=>{throw "compare";});}catch(error){console.log("throw",error,thrown.join("|"));}
try{(numbers.sort as any)(42);}catch(error){console.log("invalid",(error as Error).name);}
console.log("metadata",Array.prototype.sort.name,Array.prototype.sort.length);
