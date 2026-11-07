const numbers=[1,2];console.log(numbers.concat([3,4],5).join("|"),numbers.join("|"));console.log(numbers.concat()===numbers,numbers.concat().join("|"));
const sparse:any[]=[,1,,];const other:any[]=[2,,3];const combined=sparse.concat(other,undefined,null,["x",["y"]] as any);console.log(combined.length,Object.keys(combined).join("|"),JSON.stringify(combined),JSON.stringify(sparse));
const concat:any=Array.prototype.concat;const object={0:"x",length:1};const result=concat.call(object,[1,2],"hi",object);console.log(result.length,result[0]===object,result[4]===object,JSON.stringify(result));
const boxed=concat.call("ab",[1]);console.log(typeof boxed[0],String(boxed[0]),boxed[1],boxed.length);
const inherited:any[]=[,1];Object.setPrototypeOf(inherited,Object.assign(Object.create(Array.prototype),{0:"inherited"}));const copy=concat.call(inherited,[2]);console.log(copy.join("|"),Object.keys(copy).join("|"));
console.log("metadata",concat.name,concat.length);
