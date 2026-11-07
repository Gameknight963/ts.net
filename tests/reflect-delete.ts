function dictionary(value:any):any {return Object.assign(Object.create(null),value);}
const direct={x:1,y:2};
console.log('direct',Reflect.deleteProperty(direct,'x'),Object.hasOwn(direct,'x'),direct.x,direct.y,Object.keys(direct).join(','));
const remove=Reflect.deleteProperty;
const alias=dictionary({x:3,y:4});console.log('method alias',remove(alias,'y'),Object.keys(alias).join(','),alias.y);
const reflection=Reflect;
const namespaceAlias=dictionary({x:5});console.log('namespace alias',reflection.deleteProperty(namespaceAlias,'x'),Object.keys(namespaceAlias).length);
const {deleteProperty:destructured}=Reflect;
const destructuredTarget=dictionary({x:6});console.log('destructured',destructured(destructuredTarget,'x'),Object.hasOwn(destructuredTarget,'x'));
const computed={x:7};console.log('computed',Reflect['deleteProperty'](computed,'x'),Object.keys(computed).length);
class Example {value=8;}
const instance=new Example();console.log('class',Reflect.deleteProperty(instance,'value'),instance.value,Object.keys(instance).length);
const array=[1,2,3];console.log('array',Reflect.deleteProperty(array,'1'),array.length,Object.hasOwn(array,'1'),array[1],Reflect.deleteProperty(array,'length'));
const described=Object.create(null,{locked:{value:1,enumerable:true},open:{value:2,enumerable:true,configurable:true},get:{get:()=>{throw new Error('getter invoked');},configurable:true}});
console.log('descriptors',remove(described,'locked'),remove(described,'open'),remove(described,'get'),remove(described,'missing'),Object.keys(described).join(','));
console.log('protected',remove(Math,'PI'),remove(Array,'prototype'));

let effects='';const key:any={toString():string {effects+='K';return 'x';}};
const keyed=dictionary({x:1});console.log('key',remove(keyed,key),effects,Object.keys(keyed).length);
for(const target of [null,undefined,'text',3,true]) {effects='';try{remove(target as any,key);}catch(error){console.log('invalid',(error as Error).name,effects);}}
console.log('string property',remove(Object('text'),'0'),remove(Object('text'),'length'));
const raw=(JSON as any).rawJSON('1');console.log('raw',remove(raw,'rawJSON'),remove(raw,'missing'));
function value():void {}
console.log('function',remove(value,'name'),remove(value,'prototype'),value.name);
const descriptor=Object.getOwnPropertyDescriptor(Reflect,'deleteProperty')!;
console.log('metadata',descriptor.value.name,descriptor.value.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);
function shadow(Reflect:any):void {console.log('shadow',Reflect.deleteProperty());}shadow({deleteProperty:()=> 'local'});
