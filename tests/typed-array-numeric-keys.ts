export {};
function read(target:ArrayLike<number>,key:string):number{return target[key as any];}
for(const Constructor of [Int8Array,Uint8Array,Uint8ClampedArray,Int16Array,Uint16Array,Int32Array,Uint32Array,Float32Array,Float64Array]){
    const values:any=new Constructor([3,4]);
    values["01"]=17;values["1.0"]=18;values["1e0"]=19;values[" 1"]=20;
    console.log(Constructor.name,["0","1","2","-0","NaN","Infinity","4294967295","00","01","1.0","1e0"," 1"].map(key=>String(read(values,key))).join(","));
}
const buffer=new ArrayBuffer(32,{maxByteLength:64});
const tracking=new Float64Array(buffer),fixed=new Float64Array(buffer,0,3);
tracking[0]=7;tracking[2]=11;
console.log("initial",read(tracking,"2"),read(fixed,"2"));
buffer.resize(8);console.log("shrunk",read(tracking,"2"),read(fixed,"0"));
buffer.resize(40);console.log("grown",read(tracking,"2"),read(fixed,"0"));
buffer.transfer();console.log("detached",read(tracking,"0"),read(fixed,"0"));
const custom=new Float64Array([3,4]);let gets=0;
Object.defineProperty(custom,"length",{get(){gets++;return 9;},configurable:true});
console.log("custom length",read(custom,"length"),gets);
const prototype=Object.create(Object.getPrototypeOf(custom));
Object.defineProperty(prototype,"2",{get(){throw new Error("numeric index must not use prototype");},configurable:true});
Object.setPrototypeOf(custom,prototype);
console.log("numeric prototype",read(custom,"2"));
