for(const pattern of [/a(b)?/g,/(?:)/g,/a/gy,/z/g,"a",undefined]){
 const values=[..."ab a".matchAll(pattern as any)];console.log(JSON.stringify(values));
 for(const value of values)console.log(value.index,value.input,value.groups,value[1]);
}
const regex=/a/g;regex.lastIndex=1;const iterator="aba".matchAll(regex);regex.lastIndex=100;
console.log("first",JSON.stringify(iterator.next()));console.log("end",JSON.stringify(iterator.next()),JSON.stringify(iterator.next()),regex.lastIndex);
const reuse="aaa".matchAll(/a/g);console.log("take",reuse.next().value?.index);for(const match of reuse)console.log("rest",match.index);
console.log("array from",JSON.stringify(Array.from("abc".matchAll(/(?:)/g))));
for(const index of [-1,NaN,Infinity,100,1.9]){const pattern=/a/g;pattern.lastIndex=index;console.log("index",index,JSON.stringify([..."aaa".matchAll(pattern as any)]),pattern.lastIndex);}
try{"abc".matchAll(/a/);}catch(error){console.log("nonglobal",(error as Error).name);}
const convert=/a/g;(convert as any).lastIndex={valueOf(){console.log("convert");return 1;}};const pending="aaa".matchAll(convert);console.log("created");console.log(pending.next().value?.index);
console.log("generic",JSON.stringify([...(String.prototype.matchAll as any).call(123,/2/g)]));
console.log("units",[..."??".matchAll(/(?:)/g)].length);
console.log("metadata",String.prototype.matchAll.name,String.prototype.matchAll.length);
