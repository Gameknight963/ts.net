export {};
class Updates {value=1;constructor(){console.log(this.value++,++this.value,this.value--,--this.value,this.value);}}
new Updates();
class Missing {value!:number;constructor(){console.log(this.value++,this.value,Number.isNaN(this.value));}}
new Missing();
class Signed {value=-0;constructor(){console.log(Object.is(this.value++,-0),this.value);this.value=-0;console.log(Object.is(this.value--,-0),this.value);}}
new Signed();
const events:string[]=[];
class Accessor {
 value=1;
 constructor(){
  let stored=2;
  Object.defineProperty(this,'value',{configurable:true,get(){events.push('get');return stored;},set(value:number){events.push('set:'+value);stored=value;}});
  console.log(this.value++,++this.value,this.value--,--this.value,stored,events.join(','));
 }
}
new Accessor();
class Inherited {declare value:number;constructor(){Object.setPrototypeOf(this,{get value(){events.push('inherited get');return 4;},set value(value:number){events.push('inherited set:'+value);}});console.log(this.value++,Object.hasOwn(this,'value'));}}
new Inherited();console.log(events.slice(-2).join(','));
class Frozen {value=1;constructor(){Object.freeze(this);try{this.value++;}catch(error){console.log('frozen',(error as Error).name,this.value);}}}
new Frozen();
class Dynamic {value:any=1;constructor(){if(typeof this.value==='number'){console.log(this.value++,++this.value,this.value);}}}
new Dynamic();
class Big {value:any=1n;constructor(){console.log(String(this.value++),String(++this.value));}}
new Big();
class Mutable {value=1;increment(){return this.value++;}}
const mutable=new Mutable();console.log(mutable.increment(),mutable.value);
const other={value:5};console.log(Mutable.prototype.increment.call(other),other.value);
export function work(count:number):number {
 class Counter {value=0;constructor(count:number){for(let i=0;i<count;i++)this.value++;}}
 return new Counter(count).value;
}
console.log(work(10));
function updateRecord(record:{[key:string]:number}):void {
 console.log(record.value++,++record.value,record.value--,--record.value,record.value);
}
const parsed=JSON.parse('[{"a":1,"b":2,"c":3,"d":4,"value":5}]')[0];
updateRecord(parsed);
parsed.value=undefined;
updateRecord(parsed);
console.log(Number.isNaN(parsed.value),Object.hasOwn(parsed,'value'));
delete parsed.value;
Object.setPrototypeOf(parsed,{value:8});
updateRecord(parsed);
console.log(Object.hasOwn(parsed,'value'));
const intercepted=JSON.parse('[{"value":1,"text":"mixed"}]')[0];
let stored=3;
Object.defineProperty(intercepted,'value',{get(){events.push('record get');return stored;},set(value:number){events.push('record set:'+value);stored=value;},configurable:true});
updateRecord(intercepted);
console.log(stored,events.slice(-9).join(','));
Object.freeze(parsed);
try{updateRecord(parsed);}catch(error){console.log('record frozen',(error as Error).name);}
