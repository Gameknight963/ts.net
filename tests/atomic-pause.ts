export {};
const pause:any=(Atomics as any).pause;
console.log("pause",pause(),pause.call(null),pause.call({}));
let calls=0;
function argument():number{calls++;return 1;}
console.log("arguments",pause(argument()),calls);
const object={pause};console.log("receiver",object.pause());
const descriptor=Object.getOwnPropertyDescriptor(Atomics,"pause")!;
console.log("metadata",pause.name,pause.length,descriptor.writable,descriptor.enumerable,descriptor.configurable,Object.keys(Atomics).includes("pause"));
try{new pause();}catch(error){console.log("constructor",(error as Error).name);}
let checksum=0;
for(let index=0;index<100;index++){pause();checksum+=index;}
console.log("loop",checksum);
for (const hint of [undefined,0,-0,-1,1,1e20,Number.MAX_VALUE]) console.log('hint',pause(hint));
let coercions = 0;
const coercible = {valueOf() {coercions++;return 1;},toString() {coercions++;return '1';}};
for (const hint of [0.5,NaN,Infinity,-Infinity,'1',null,true,1n,Symbol('hint'),coercible,new Number(1)]) {
    try {pause(hint);} catch (error) {console.log('invalid hint',(error as Error).name);}
}
console.log('no coercion',coercions);
