console.log('array',Array.from([1,2,3]).join(','));
console.log('string',Array.from('A\u{1F600}B').join('|'));
console.log('boxed string',Array.from(new String('A\u{1F600}B')).length);
console.log('holes',Array.from([,2,,]).join(','),Object.keys(Array.from([,2,,])).join(','));
console.log('arraylike',Array.from({0:'a',2:'c',length:3}).join(','));
console.log('number',Array.from(3 as any).length);
console.log('mapping',Array.from([2,4],function(this:{offset:number},value,index){console.log('map args',arguments.length);return this.offset+value+index;},{offset:10}).join(','));
const growing=[1,2];console.log('growing',Array.from(growing,(v,i)=>{if(i===0)growing.push(3);return v;}).join(','));
const shrinking=[1,2,3];console.log('shrinking',Array.from(shrinking,(v,i)=>{if(i===0)shrinking.length=1;return v;}).join(','));
const arraylike:any={0:1,1:2,length:2};console.log('snapshot',Array.from(arraylike,(v,i)=>{arraylike.length=0;return v;}).join(','));
class Derived<T> extends Array<T>{}
const derived=Derived.from([3,4]);console.log('derived',derived instanceof Derived,derived.join(','));
class Recorded {length=0;constructor(...args:any[]){console.log('constructor',args.length,args[0]);}}
const generic:any=(Array.from as any).call(Recorded,{0:'x',length:1});console.log('generic',generic instanceof Recorded,generic[0],generic.length);
(Array.from as any).call(Recorded,[1,2]);
const from=Array.from;console.log('detached',from([5]).join(','));
try{Array.from(null as any);}catch(e){console.log('null',e instanceof TypeError);}
try{Array.from([],1 as any);}catch(e){console.log('bad mapper',e instanceof TypeError);}
try{Array.from([1],()=>{throw 'mapper';});}catch(e){console.log('mapper throws',e);}
console.log('metadata',Array.from.name,Array.from.length);
const inherited:any=[,2];const proto={get 0():number{console.log('inherited getter');return 7;}};Object.setPrototypeOf(proto,Array.prototype);Object.setPrototypeOf(inherited,proto);console.log('inherited',Array.from(inherited).join(','));
const ordered={get length():number{console.log('length getter');return 2;},get 0():number{console.log('first getter');return 8;},get 1():number{console.log('second getter');return 9;}};
console.log('ordered',Array.from(ordered,(value,index)=>{console.log('mapping index',index);return value;}).join(','));
try{Array.from(ordered,1 as any);}catch(e){console.log('mapper before length',e instanceof TypeError);}
console.log('fractional',Array.from({0:'a',1:'b',length:1.9}).join(','));
console.log('negative',Array.from({length:-2}).length);
console.log('nan',Array.from({length:NaN}).length);
function fromArguments(...unused:number[]):void{console.log('arguments',Array.from(arguments).join(','));}
fromArguments(4,5);
const getterFailure={get length():number{throw 'length failed';}};
try{Array.from(getterFailure);}catch(e){console.log('length throws',e);}
const elementFailure={length:1,get 0():number{throw 'element failed';}};
try{Array.from(elementFailure);}catch(e){console.log('element throws',e);}
class Throwing {constructor(...args:any[]){throw 'construction failed';}}
try{(Array.from as any).call(Throwing,[]);}catch(e){console.log('constructor throws',e);}
const setterPrototype={set 0(value:number){console.log('unexpected setter',value);}};
class OwnProperty {constructor(){Object.setPrototypeOf(this,setterPrototype);}}
const own:any=(Array.from as any).call(OwnProperty,[6]);console.log('own property',Object.hasOwn(own,'0'),own[0],own.length);
const packedSource=[1,2,3];const mutable:any=Array.from(packedSource);mutable[0]='text';mutable.push(true);console.log('independent writable result',mutable.join(','),packedSource.join(','));
const special=Array.from([-0,NaN,Infinity]);console.log('numeric values',1/special[0],Number.isNaN(special[1]),special[2]);
const existing:any=[];
class ExistingArray {constructor(){return existing;}}
const same:any=(Array.from as any).call(ExistingArray,[7,8]);console.log('existing result',same===existing,same.join(','));
