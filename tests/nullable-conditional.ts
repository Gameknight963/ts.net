export {};
let order = '';
function condition(pick: boolean): boolean {order+='C';return pick;}
function numeric(value: number): number {order+='N';return value;}
function choose(pick: boolean, value: number): number | undefined {return condition(pick) ? numeric(value) : undefined;}
for(const value of [0,-0,NaN,Infinity,-Infinity,1.25]) {
 for(const pick of [false,true]) {
  order='';const chosen=choose(pick,value);
  console.log('choice',typeof chosen,chosen===undefined,Object.is(chosen,-0),Number.isNaN(chosen),chosen===value,order);
 }
}
let setterCalls=0, stored: number | undefined;
const target={set value(value: number | undefined){setterCalls++;stored=value;}};
for(const pick of [false,true]) {order='';const result=(target.value=condition(pick)?numeric(7):undefined);console.log('setter',result,stored,setterCalls,order);}
function shadow(undefined: number): number | undefined {return condition(true)?undefined:0;}
console.log('shadow',shadow(9));
function nested(a:boolean,b:boolean): number|undefined{return a?(b?3:undefined):undefined;}
console.log('nested',nested(false,false),nested(true,false),nested(true,true));
let defaults=0;
function fallback(value: number=(++defaults,11)):number{return value;}
console.log('defaults',fallback(undefined),fallback(),defaults);
function throws():number{throw 'chosen error';}
console.log('unchosen',false?throws():undefined);
try {console.log(choose(true, (():number=>{throw 'argument error';})()));}catch(error){console.log('caught',error);}

function effect():number{order+='V';return 13;}
order='';const voidInitial: number|undefined=void effect();console.log('void initial',voidInitial,order);
function voidChoice(pick:boolean):number|undefined{return condition(pick)?numeric(5):void effect();}
for(const pick of [false,true]){order='';console.log('void choice',voidChoice(pick),order);}
order='';console.log('void argument',fallback(void effect()),defaults,order);
