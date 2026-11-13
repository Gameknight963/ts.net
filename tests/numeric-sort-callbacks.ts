/// <reference lib="es2023.array" />
export {};
const values=[4,1,3,2,1];
const ascending=(a:number,b:number):number=>a-b;
console.log('sorted',values.toSorted(ascending).join(','),values.join(','));
const mutable=values.slice();console.log('sort',mutable.sort(ascending)===mutable,mutable.join(','));
let calls=0;
function argumentsComparator(a:number,b:number):number {
 if(arguments.length!==2 || arguments[0]!==a || arguments[1]!==b || arguments[2]!==undefined)throw new Error('arguments');
 calls++;return a-b;
}
console.log('arguments',values.toSorted(argumentsComparator).join(','),calls>0);
function receiverComparator(this:unknown,a:number,b:number):number {if(this!==undefined)throw new Error('receiver');return b-a;}
console.log('receiver',values.toSorted(receiverComparator).join(','));
function restComparator(...args:number[]):number {if(args.length!==2)throw new Error('rest');return args[0]-args[1];}
console.log('rest',values.toSorted(restComparator).join(','));
function make(this:{direction:number}) {return (a:number,b:number):number=>this.direction*(a-b);}
console.log('capture',values.toSorted(make.call({direction:-1})).join(','));
const equal=(a:number,b:number):number=>NaN;console.log('equal',values.toSorted(equal).join(','));
console.log('constant',values.toSorted(()=>0).join(','));
const signed=[-0,0,-0,0].toSorted(ascending);console.log('zero',signed.map(x=>Object.is(x,-0)?'-0':'0').join(','));
const records=[{key:1,id:'a'},{key:0,id:'b'},{key:1,id:'c'}];console.log('records',records.toSorted((a,b)=>a.key-b.key).map(x=>x.id).join(','));
const holes:number[]=[3,,1,undefined as any];console.log('holes',JSON.stringify(holes.toSorted(ascending)),JSON.stringify(holes.sort(ascending)),Object.hasOwn(holes,3));
const special=new Float64Array([NaN,0,-0,3,-1,NaN]);special.sort();console.log('typed default',Array.from(special,x=>Number.isNaN(x)?'NaN':Object.is(x,-0)?'-0':String(x)).join(','));
console.log('typed callback',Array.from(new Float64Array([4,2,3,1]).toSorted(ascending)).join(','));
try {values.toSorted((a:number,b:number):number=>{throw new Error('comparison');});}catch(error){console.log('throw',(error as Error).message);}
