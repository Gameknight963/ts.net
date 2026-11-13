export {};
const method:string='deleteProperty';
const reflect:any=Reflect;
const object={first:1,second:2};const alias=object;
console.log('computed',reflect[method](object,'first'),Object.hasOwn(alias,'first'),Object.keys(alias).join(','),alias===object);
object.first=3;console.log('reinsert',Object.keys(alias).join(','),alias.first);
const remove=reflect[method];
const direct={first:4};console.log('alias',remove(direct,'first'),Object.keys(direct).length);
const called={first:5};console.log('call',remove.call(null,called,'first'),Object.hasOwn(called,'first'));
const applied={first:6};const argumentsList:[object,string]=[applied,'first'];
console.log('apply',Reflect.deleteProperty.apply(null,argumentsList),Object.hasOwn(applied,'first'));
const spread={first:7};const spreadArguments:any[]=[spread,'first'];
console.log('spread apply',(Reflect.deleteProperty as any).apply(null,[...spreadArguments]),Object.hasOwn(spread,'first'));
const reflected={first:8};const reflectedArguments:[object,string]=[reflected,'first'];
console.log('reflect apply',Reflect.apply(Reflect.deleteProperty,null,reflectedArguments),Object.hasOwn(reflected,'first'));
const computedApply:string='apply';const ambiguous={first:9};
console.log('computed apply',reflect[computedApply](Reflect.deleteProperty,'unusedThis',[ambiguous,'first']),Object.hasOwn(ambiguous,'first'));
const boundTarget={first:10};const bound=remove.bind(null,boundTarget);
console.log('bound target',bound('first'),Object.hasOwn(boundTarget,'first'));
const boundMethod=remove.bind(null);const unboundTarget={first:11};
console.log('bound method',boundMethod(unboundTarget,'first'),Object.hasOwn(unboundTarget,'first'));
function choose(flag:boolean):any{return flag?reflect[method]:Reflect.deleteProperty;}
const selected={first:12};console.log('returned',choose(true)(selected,'first'),Object.hasOwn(selected,'first'));
function invoke(fn:any,target:any,key:string):boolean{return fn(target,key);}
const parameter={first:13};console.log('parameter',invoke(remove,parameter,'first'),Object.hasOwn(parameter,'first'));
function namespace(ns:any,name:string,target:any,key:string):boolean{return ns[name](target,key);}
const namespaceTarget={first:14};console.log('namespace',namespace(Reflect,method,namespaceTarget,'first'),Object.hasOwn(namespaceTarget,'first'));
const symbol=Symbol('field');const symbolic={first:15,[symbol]:16};
console.log('symbol',remove(symbolic,symbol),Object.getOwnPropertySymbols(symbolic).length,symbolic.first);
let conversions=0;const coercion={first:17};
const key:any={[Symbol.toPrimitive](){conversions++;return 'first';}};
console.log('key',remove(coercion,key),conversions,Object.hasOwn(coercion,'first'));
const inherited={first:18};Object.setPrototypeOf(inherited,{first:19});
console.log('inherited',remove(inherited,'first'),inherited.first,Object.hasOwn(inherited,'first'));
