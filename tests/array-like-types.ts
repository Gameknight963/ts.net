export {};
function sum(values:ArrayLike<number>):number{let total=0;for(let index=0;index<values.length;index++)total+=values[index];return total;}
function text(values:ArrayLike<string>):string{let result="";for(let index=0;index<values.length;index++)result+=values[index];return result;}
console.log("arrays",sum([1,2,3]),sum(new Uint8Array([4,5,6])),sum(new Float64Array([1.5,2.5])));
console.log("strings",text("hello"),text(["a","b","c"]));
const numeric:ArrayLike<number>={0:7,1:8,length:2};console.log("object",sum(numeric));
function forward<T>(value:ArrayLike<T>):ArrayLike<T>{return value;}
const array=new Uint16Array([10,20]);console.log("identity",forward(array)===array,sum(forward(array)));
const values:readonly number[]=[1,2,3];console.log("readonly",sum(values));
console.log("from",Array.from(forward(array)).join(","),Array.from("ab" as ArrayLike<string>).join(","));
