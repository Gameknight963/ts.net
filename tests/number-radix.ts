for(const radix of [2,3,4,8,10,16,32,36])for(const value of [0,-0,1,-1,255,-255,65535,9007199254740991,NaN,Infinity,-Infinity])console.log(radix,value.toString(radix));
for(const radix of [2,4,8,16,32])for(const value of [0.5,0.1,-3.125,Number.MIN_VALUE,2**100,Number.MAX_VALUE])console.log("binary",radix,value.toString(radix));
const method:any=Number.prototype.toString;console.log("boxed",new Number(255).toString(16),method.call(255,"16"),method.call(1),method.call(1,undefined));
for(const radix of [1,37,NaN,Infinity,null])try{method.call(NaN,radix);}catch(error){console.log("invalid",(error as Error).name);}
for(const receiver of ["1",true,{},null,undefined])try{method.call(receiver,16);}catch(error){console.log("receiver",(error as Error).name);}
let calls=0;console.log("conversion",method.call(255,{valueOf(){calls++;return 16;}}),calls);
console.log("metadata",method.name,method.length);
