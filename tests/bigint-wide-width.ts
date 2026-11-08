export {};
for(const bits of [2147483648,4294967296,Number.MAX_SAFE_INTEGER]) {
 for(const value of [0n,1n,-1n,12345678901234567890n,-12345678901234567890n]) {
  console.log('signed',bits,BigInt.asIntN(bits,value).toString());
  if(value>=0n)console.log('unsigned',bits,BigInt.asUintN(bits,value).toString());
 }
}
for(const bits of [-1,Infinity,Number.MAX_SAFE_INTEGER+1]) {
 let reads=0;const value:any={valueOf(){reads++;return 1n;}};
 try{BigInt.asIntN(bits,value);}catch(error){console.log('invalid width',(error as Error).name,reads);}
}
let reads=0;const value:any={valueOf(){reads++;return -1n;}};
try{BigInt.asUintN(Number.MAX_SAFE_INTEGER,value);}catch(error){console.log('oversized result',(error as Error).name,reads);}
let order='';const bits:any={valueOf(){order+='bits|';return Number.MAX_SAFE_INTEGER;}};
const number:any={valueOf(){order+='value|';return 42n;}};
console.log('order',BigInt.asIntN(bits,number).toString(),order);
const marker=new Error('conversion');
try{BigInt.asIntN(Number.MAX_SAFE_INTEGER,{valueOf(){throw marker;}} as any);}catch(error){console.log('throw',error===marker);}
