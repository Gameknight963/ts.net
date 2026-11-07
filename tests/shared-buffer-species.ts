export {};
const source=new SharedArrayBuffer(4);new Uint8Array(source).set([1,2,3,4]);
(source as any).constructor={[Symbol.species]:function(length:number){const result=new SharedArrayBuffer(length+2);new Uint8Array(result).fill(9);return result;}};
console.log("larger",Array.from(new Uint8Array(source.slice(1,3))).join("|"));
for(const kind of ["self","alias","ordinary","short"]){const buffer=new SharedArrayBuffer(4);(buffer as any).constructor={[Symbol.species]:function(){return kind==="self"?buffer:kind==="alias"?structuredClone(buffer):kind==="ordinary"?new ArrayBuffer(4):new SharedArrayBuffer(1);}};try{buffer.slice(0,3);}catch(error){console.log(kind,(error as Error).name);}}
const cloned=structuredClone(new DataView(source,1,2));cloned.setUint8(0,42);console.log("view-clone",new Uint8Array(source)[1],cloned.buffer instanceof SharedArrayBuffer);
