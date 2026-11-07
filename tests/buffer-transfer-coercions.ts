const resized=new ArrayBuffer(4,{maxByteLength:8});new Uint8Array(resized).set([1,2,3,4]);
const grown=resized.transfer({valueOf(){resized.resize(2);return 6;}} as any);console.log("coercion",Array.from(new Uint8Array(grown)).join("|"),resized.detached);
const taken=new ArrayBuffer(4);try{taken.transfer({valueOf(){taken.transfer();return 2;}} as any);}catch(error){console.log("reentrant",(error as Error).name,taken.detached);}
const buffer=new ArrayBuffer(4);const array=new Uint8Array(buffer);array[0]={valueOf(){buffer.transfer();return 9;}} as any;console.log("write",array[0],array.length);
const viewed=new ArrayBuffer(4);const view=new DataView(viewed);try{view.setUint8(0,{valueOf(){viewed.transfer();return 1;}} as any);}catch(error){console.log("write-view",(error as Error).name);}
for(const value of [-1,Infinity]){const source=new ArrayBuffer(4);try{source.transfer(value);}catch(error){console.log("invalid",(error as Error).name,source.detached);}}
