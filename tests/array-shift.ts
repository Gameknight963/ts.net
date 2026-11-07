const numbers=[2,3];console.log(numbers.unshift(0,1),numbers.join("|"));console.log(numbers.shift(),numbers.join("|"));console.log(numbers.unshift(),numbers.join("|"));
while(numbers.length)console.log("drain",numbers.shift(),numbers.length);console.log("empty",numbers.shift(),numbers.length);
const sparse:any[]=[,1,,3];console.log("sparse",sparse.shift(),sparse.length,Object.keys(sparse).join("|"),sparse.join("|"));console.log(sparse.unshift("a","b"),Object.keys(sparse).join("|"),sparse.join("|"));
const shift:any=Array.prototype.shift,unshift:any=Array.prototype.unshift;
const object:any={0:"a",2:"c",length:3};console.log("object",shift.call(object),JSON.stringify(object));console.log(unshift.call(object,"x","y"),JSON.stringify(object));
const inherited:any=Object.create({1:"b"});inherited[0]="a";inherited.length=3;console.log("inherited",shift.call(inherited),inherited[0],inherited[1],inherited.length,Object.keys(inherited).join("|"));
console.log("missing",shift.call({}),unshift.call({},1));
try{shift.call("abc");}catch(error){console.log("readonly",(error as Error).name);}
try{unshift.call({length:9007199254740991},1);}catch(error){console.log("overflow",(error as Error).name);}
console.log("metadata",shift.name,shift.length,unshift.name,unshift.length);
