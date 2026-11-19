export {};
class Counter {value=0;constructor(count:number){for(let i=0;i<count;i++)this.value++;}}
for(const count of [0,1,31,32,64,64.5,NaN])console.log('count',count,new Counter(count).value);
class Subtract {value=100;constructor(count:number){for(let i=0;i<count;i++)this.value-=0.25;}}
console.log('subtract',new Subtract(64).value);
class Decrement {value=100;constructor(count:number){for(let i=0;i<count;i++)this.value--;}}
console.log('decrement',new Decrement(64).value);
class Missing {value:number=undefined as any;constructor(count:number){for(let i=0;i<count;i++)this.value++;}}
console.log('missing',new Missing(0).value,Number.isNaN(new Missing(64).value));
class NegativeZero {value=-0;constructor(count:number){for(let i=0;i<count;i++)this.value+=0;}}
console.log('negative zero',Object.is(new NegativeZero(0).value,-0),Object.is(new NegativeZero(64).value,-0));
class Frozen {value=1;constructor(count:number){Object.freeze(this);for(let i=0;i<count;i++)this.value++;}}
console.log('empty frozen',new Frozen(0).value);
let threw=false;try{new Frozen(64);}catch{threw=true;}console.log('frozen',threw);
class ReadOnly {value=1;constructor(count:number){Object.defineProperty(this,'value',{writable:false});for(let i=0;i<count;i++)this.value++;}}
console.log('empty read-only',new ReadOnly(0).value);
threw=false;try{new ReadOnly(64);}catch{threw=true;}console.log('read-only',threw);
let gets=0,sets=0;
class Accessor {value=0;constructor(count:number){
    Object.defineProperty(this,'value',{get(){gets++;return 5;},set(value:number){sets++;},configurable:true});
    for(let i=0;i<count;i++)this.value++;
}}
new Accessor(64);console.log('accessor',gets,sets);
class Local {value=0;}
function update(value:Local,count:number):number {for(let i=0;i<count;i++)value.value++;return value.value;}
const local=new Local();console.log('local',update(local,64),update(local,0));
console.log('structural',update({value:5},64));
const shared=new Local();function updateGlobal(count:number):number {for(let i=0;i<count;i++)shared.value+=0.5;return shared.value;}
console.log('global',updateGlobal(64),updateGlobal(0));
const alias=shared;console.log('alias',alias.value);
let inherited=0;
Object.defineProperty(Counter.prototype,'value',{configurable:true,get(){return 9;},set(value:number){inherited++;}});
try{console.log('own field',new Counter(64).value,inherited);}finally{delete (Counter.prototype as any).value;}
class Parent {value=0;constructor(count:number){for(let i=0;i<count;i++)this.value++;}}
class Derived extends Parent {extra=7;}
const derived=new Derived(64);console.log('derived',derived.value,derived.extra);
