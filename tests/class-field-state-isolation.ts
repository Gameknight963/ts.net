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
class Mixed {number=1;flag=true;text="initial";}
const mixed=new Mixed(),untouched=new Mixed();
for(let i=0;i<8;i++) {
    mixed.number=i%2===0 ? undefined as unknown as number : i;
    mixed.flag=i%3===0 ? undefined as unknown as boolean : i%2===0;
    mixed.text=i%3===0 ? undefined as unknown as string : i%3===1 ? null as unknown as string : "changed";
    console.log(mixed.number,mixed.flag,mixed.text,Object.hasOwn(mixed,"number"),untouched.number,untouched.flag,untouched.text);
}
for(const source of ['{"number":1,"flag":true,"text":"initial"}','{"number":1,"other":2}']) {
    const parsed=JSON.parse("["+source+","+source+"]");
    parsed[0].number=undefined;
    parsed[1].number=9;
    console.log(parsed[0].number,parsed[1].number,Object.hasOwn(parsed[0],"number"),JSON.stringify(parsed));
    delete parsed[0].number;
    Object.defineProperty(parsed[1],"number",{get(){return 12;},configurable:true,enumerable:true});
    console.log(Object.hasOwn(parsed[0],"number"),parsed[1].number,JSON.stringify(parsed));
}
const wideSource="{"+Array.from({length:32},(_,i)=>'"field'+i+'":'+i).join(",")+"}";
const wide=JSON.parse("["+wideSource+","+wideSource+"]");
wide[0].field31=undefined;
wide[1].field31=50;
console.log(wide[0].field31,wide[1].field31,wide[0].field30,wide[1].field30,Object.keys(wide[0]).length);
