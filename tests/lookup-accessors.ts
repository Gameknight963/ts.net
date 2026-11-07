const getterLookup:any=(Object.prototype as any).__lookupGetter__;
const setterLookup:any=(Object.prototype as any).__lookupSetter__;
let calls=0;
const getter=()=>{calls++;return 1;};
const setter=(value:number)=>{calls+=value;};
const prototype=Object.create(null,{value:{get:getter,set:setter},empty:{get:undefined,set:undefined},onlyGet:{get:getter},onlySet:{set:setter}});
const child=Object.create(prototype);
for(const source of [prototype,child]) {
    console.log('identity',getterLookup.call(source,'value')===getter,setterLookup.call(source,'value')===setter);
    console.log('partial',getterLookup.call(source,'onlyGet')===getter,setterLookup.call(source,'onlyGet'),getterLookup.call(source,'onlySet'),setterLookup.call(source,'onlySet')===setter);
    console.log('absent',getterLookup.call(source,'missing'),setterLookup.call(source,'missing'),getterLookup.call(source,'empty'),setterLookup.call(source,'empty'));
}
const shadowed=Object.create(prototype,{value:{value:3}});
console.log('shadowed',getterLookup.call(shadowed,'value'),setterLookup.call(shadowed,'value'));
const emptyShadow=Object.create(prototype,{value:{get:undefined,set:undefined}});
console.log('empty shadow',getterLookup.call(emptyShadow,'value'),setterLookup.call(emptyShadow,'value'));
const literal={get value():number {throw new Error('getter invoked');},set value(value:number){throw new Error('setter invoked');}};
console.log('literal',typeof getterLookup.call(literal,'value'),typeof setterLookup.call(literal,'value'));
class Base {get field():number {throw new Error('class getter invoked');}}
class Child extends Base {}
console.log('class',getterLookup.call(new Child(),'field')===Object.getOwnPropertyDescriptor(Base.prototype,'field')!.get);
console.log('calls',calls);
let effects='';
const key={toString():string {effects+='K';return 'value';}};
console.log('key',getterLookup.call(child,key)===getter,effects);
for(const source of [null,undefined])for(const method of [getterLookup,setterLookup]) {
    effects='';try{method.call(source,key);}catch(error){console.log('nullish',(error as Error).name,effects);}
}
for(const source of ['text',3,false,Object('text'),[1]])console.log('primitives',getterLookup.call(source,'length'),setterLookup.call(source,'length'));
for(const method of [getterLookup,setterLookup]) {
    console.log('metadata',method.name,method.length,Object.prototype.propertyIsEnumerable.call(Object.prototype,method.name));
    console.log('omitted',method.call({undefined:1}));
}
const alias={__lookupGetter__:getterLookup,__lookupSetter__:setterLookup};
Object.setPrototypeOf(alias,prototype);
console.log('method receiver',alias.__lookupGetter__('value')===getter,alias.__lookupSetter__('value')===setter);


function inspectArguments(value:number):void {
    const descriptor=Object.getOwnPropertyDescriptor(arguments,'callee')!;
    console.log('arguments',getterLookup.call(arguments,'callee')===descriptor.get,setterLookup.call(arguments,'callee')===descriptor.set);
}
inspectArguments(1);
