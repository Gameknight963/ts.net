let effects='';function key():string{effects+='K';return 'value';}function argument():number{effects+='A';return 3;}
const missing:any=null;console.log(missing?.value,missing?.[key()],missing?.method(argument()),effects);
const absent:any=undefined;console.log(absent?.x?.y,absent?.(argument()),effects);
const present:any={value:5,nested:{value:7},method(this:any,next:number){return this.value+next;}};
console.log(present?.value,present?.[key()],present?.method(argument()),effects);
console.log(present?.nested.value,present?.nested?.value,present.method?.(2),(present?.method)(4));
const noMethod:any={value:1};console.log(noMethod.method?.(argument()),effects);
try{noMethod?.method(argument());}catch(error){console.log(error instanceof TypeError,effects);}
try{(missing?.nested).value;}catch(error){console.log(error instanceof TypeError);}
try{(missing?.method)(argument());}catch(error){console.log(error instanceof TypeError,effects);}
const comma:any=(argument(),present?.method);try{comma(1);}catch(error){console.log(error instanceof TypeError);}
let reads=0;const getter={value:8,get method(){reads++;return function(this:{value:number},next:number){return this.value+next;};}};
console.log(getter.method?.(2),reads,(getter?.method)?.(3),reads);
const add:((value:number)=>number)|undefined=(value)=>value+1;console.log(add?.(4));
function choose(value:boolean):{count:number}|null{return value?{count:6}:null;}
console.log(choose(true)?.count,choose(false)?.count,choose(false)?.count??9);
console.log('abc'?.length,'abc'?.charCodeAt(1));
console.log(console?.log===console.log);console.log?.('optional log');
const before=performance?.now();const after=performance.now?.();console.log(after>=before);
const values=[1,2];console.log(values?.at(-1),values?.length);
console.log(values?.slice(0,1)?.[0],values?.includes(2),values?.indexOf(9));console.log(values?.push(3),values?.pop(),values?.reverse()?.[0]);
console.log(Object?.keys({a:1})?.length,Object.hasOwn?.({a:1},'a'),Number?.isFinite(2),Number.isNaN?.(NaN));
console.log(Number('12'),Number(),Number(undefined),typeof Object,typeof Number);
const savedLog=console.log;let observed='';console.log=(value:any)=>{observed+=value;};console.log('A');console?.log('B');console.log=savedLog;console.log(observed);
const savedNow=performance.now;performance.now=()=>123;console.log(performance.now(),performance?.now());performance.now=savedNow;
const enum ConstOptional {Value=5}
console.log(ConstOptional?.Value);
console.log(''?.length);const falsy:any=0;console.log(falsy?.[key()],effects);try{falsy?.(argument());}catch(error){console.log(error instanceof TypeError,effects);}
let bases=0;function baseOnce(){bases++;return present;}console.log(baseOnce()?.nested.value,bases);
console.log(missing?.nested!.value);try{(missing?.nested)!.value;}catch(error){console.log(error instanceof TypeError);}
const spreadReceiver={value:10,sum(...args:number[]){return this.value+args[0]+args[1];}};
function spreadArgs():[number,number]{effects+='S';return [1,2];}
console.log(spreadReceiver?.sum?.(...spreadArgs()),missing?.sum?.(...spreadArgs()),effects);
function sumWithCallback(items:number[],step?:()=>void){let sum=0;for(let i=0;i<items.length;i++){step?.();sum+=items[i]??0;}return sum;}
const shrinking=[1,2,3];console.log(sumWithCallback(shrinking,()=>{shrinking.pop();}),shrinking.length);
const originalNaN=Number.isNaN;const numberNamespace:any=Number;numberNamespace.isNaN=()=>false;console.log(Number.isNaN(NaN),Number?.isNaN(NaN));numberNamespace.isNaN=originalNaN;
const originalKeys=Object.keys;const objectNamespace:any=Object;objectNamespace.keys=()=>['custom'];console.log(Object.keys({a:1})[0],Object?.keys({a:1})?.[0]);objectNamespace.keys=originalKeys;
let chainReads=0;
const stopEarly={get nil():any{chainReads++;return null;}};
console.log(stopEarly?.nil?.deep.value,chainReads);
class OptionalBase {value=11;read():number{return this.value;}}
class OptionalDerived extends OptionalBase {readBase():number{return super.read?.();}}
console.log(new OptionalDerived().readBase());
