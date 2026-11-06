const target={a:1};const result=Object.assign(target,{b:2},{a:3});console.log('identity',result===target,result.a,result.b,Object.keys(result).join(','));
const empty=Object.assign({},{value:4});console.log('empty target',empty.value);
const ordered=Object.assign({},{get first():number{console.log('first getter');return 1;}},{get second():number{console.log('second getter');return 2;}});console.log('ordered',ordered.first,ordered.second);
const setter={set value(value:number){console.log('setter',value);}};Object.assign(setter,{value:5});
console.log('null sources',Object.assign({a:1},null as any,undefined as any).a);
const primitive:any=Object.assign(3,{value:6});console.log('primitive',typeof primitive,primitive.value);
console.log('string source',Object.keys((Object.assign as any)({},'abc')).join(','));
try{Object.assign(null as any,{});}catch(e){console.log('null target',e instanceof TypeError);}
try{Object.assign('abc',{0:'x'});}catch(e){console.log('readonly',e instanceof TypeError);}
console.log('metadata',Object.assign.name,Object.assign.length);
const typed:{value:number}=Object.assign({},{value:7});console.log('typed result',typed.value);
const assign=Object.assign;const aliased=assign({},{alias:8});console.log('alias',aliased.alias);
function merged<T extends object,U>(target:T,source:U):T&U{return Object.assign(target,source);}
const generic=merged({},{generic:9});console.log('generic',generic.generic);
const nested=Object.assign({},{child:{value:10}});console.log('nested',nested.child.value);
const unknownSource:any={value:11};const dynamic=Object.assign({},unknownSource);const dynamicView:{value:number}=dynamic;console.log('dynamic source',dynamicView.value);
const destination={set a(value:number){console.log('write a',value);},set b(value:number){console.log('write b',value);}};
const source={get a():number{console.log('read a');return 1;},get b():number{console.log('read b');return 2;}};Object.assign(destination,source);
const proto={inherited:12};const protoTarget=Object.assign({},{['__proto__']:proto});console.log('prototype',(protoTarget as any).inherited,Object.hasOwn(protoTarget,'__proto__'));
const throwing={get a():number{return 1;},get b():number{throw 'read failed';},get c():number{console.log('unexpected read');return 3;}};const partial:any={};try{Object.assign(partial,throwing);}catch(e){console.log('partial',e,partial.a,Object.hasOwn(partial,'b'));}
const array=[0,0];Object.assign(array,{0:4,1:5});console.log('array target',array.join(','));
const fromArray=Object.assign({label:'array'},[1,2]);console.log('array source',(fromArray as any)[0],(fromArray as any)[1]);
const changing:any={get first():number{changing.second=20;Object.assign(changing,{added:30});return 10;},second:2};
const snapshot=Object.assign({},changing);console.log('snapshot',Object.keys(snapshot).join(','),snapshot.first,snapshot.second,Object.hasOwn(snapshot,'added'));
const selfTarget={value:1};console.log('self',Object.assign(selfTarget,selfTarget)===selfTarget,selfTarget.value);
const emptySources=Object.assign({value:4});console.log('no sources',emptySources.value);
const dynamicTarget:any={};const dynamicAssigned=Object.assign(dynamicTarget,{value:13});const typedDynamic:{value:number}=dynamicAssigned;console.log('dynamic target',typedDynamic.value);
const numeric=Object.assign({},{0:'zero',2:'two'});console.log('numeric',numeric[0],numeric[2],Object.keys(numeric).join(','));
const methods={value:15,method(this:{value:number}):number{return this.value;}};const copiedMethod=Object.assign({},methods);console.log('method receiver',copiedMethod.method());
const chained:{a:number,b:number}=Object.assign(Object.assign({},{a:1}),{b:2});console.log('chained',chained.a,chained.b);
const escapedArray=(Object.assign as any)({},[,2]);console.log('array source escape',Object.keys(escapedArray).join(','),escapedArray[1]);
const escapedPrimitive=(Object.assign as any)(3);console.log('primitive result escape',typeof escapedPrimitive,escapedPrimitive.valueOf());
