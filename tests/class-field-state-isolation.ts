export {};
class Entry {
    a=0;b=0;c=0;d=0;e=0;f=0;g=0;
    constructor(a:number,b:number,c:number,d:number,e:number,f:number,g:number) {
        this.a=a;this.b=b;this.c=c;this.d=d;this.e=e;this.f=f;this.g=g;
    }
}
function value(mask:number,bit:number):number {
    return mask & bit ? bit : undefined as unknown as number;
}
function make(mask:number):Entry {
    return new Entry(value(mask,1),value(mask,2),value(mask,4),value(mask,8),value(mask,16),value(mask,32),value(mask,64));
}
const entries:Entry[]=[];
for(let mask=0;mask<128;mask++) entries.push(make(mask));
let total=0,missing=0;
for(let mask=0;mask<128;mask++) {
    const item=entries[mask];
    for(const key of ["a","b","c","d","e","f","g"] as const) {
        if(item[key]===undefined)missing++;else total+=item[key];
        if(!Object.hasOwn(item,key))throw new Error("missing own field");
    }
}
console.log(total,missing);
const first=make(127),second=make(127),third=make(127);
first.a=undefined as unknown as number;
second.b=20;
console.log(first.a,first.b,second.a,second.b,third.a,third.b);
delete (first as any).c;
Object.defineProperty(second,"d",{get(){return 80;},configurable:true});
Object.setPrototypeOf(first,{g:90});
console.log(first.c,first.g,second.c,second.d,third.c,third.d,third instanceof Entry);
Object.freeze(second);
console.log(Object.isFrozen(second),Object.isFrozen(third),Object.keys(first).join(","));
third.a=30;
console.log(third.a,make(127).a,make(0).a,make(64).g);
