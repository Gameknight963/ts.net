export {};
class Arithmetic {
 value=7;
 constructor(){
  console.log(this.value+=2,this.value-=3,this.value*=4,this.value/=2,this.value%=5,this.value**=3);
  console.log(this.value|=8,this.value&=15,this.value^=3,this.value<<=1,this.value>>=2,this.value>>>=1);
 }
}
new Arithmetic();
const events:string[]=[];
class Effects {
 value=1;
 constructor(){
  let stored=3;
  Object.defineProperty(this,'value',{get(){events.push('get');return stored;},set(value:number){events.push('set:'+value);stored=value;},configurable:true});
  const rhs=()=>{events.push('rhs');stored=100;return 2;};
  console.log(this.value+=rhs(),stored,events.join(','));
 }
}
new Effects();
class Missing {value!:number;constructor(){this.value+=1;console.log(Number.isNaN(this.value));}}
new Missing();
class Signed {value=-0;constructor(){this.value*=1;console.log(Object.is(this.value,-0));this.value/=0;console.log(Number.isNaN(this.value));}}
new Signed();
class Frozen {value=1;constructor(){Object.freeze(this);try{this.value+=2;}catch(error){console.log((error as Error).name,this.value);}}}
new Frozen();
class Logical {value=0;constructor(){console.log(this.value||=2,this.value&&=3,this.value??=4);}}
new Logical();
class AnyValue {value:any=1;constructor(){this.value+=2;console.log(this.value);this.value='text';this.value+=2;console.log(this.value);}}
new AnyValue();
class BigValue {value:any=1n;constructor(){this.value+=2n;console.log(String(this.value));}}
new BigValue();
class NumberRhs {value=1;constructor(input:any){this.value+=input;console.log(this.value);}}
new NumberRhs(2);
export function work(count:number):number {
 class Counter {value=0;constructor(count:number){for(let i=0;i<count;i++)this.value+=1;}}
 return new Counter(count).value;
}
console.log(work(10));
