export {};
for (const method of ['freeze','seal','preventExtensions']) {
 const target={value:1}; const result=(Object as any)[method](target);
 console.log(method,result===target,Object.isExtensible(target),Object.isSealed(target),Object.isFrozen(target));
 console.log('add',Reflect.set(target,'extra',2),'write',Reflect.set(target,'value',3),target.value);
}
const key=Symbol('field');const target:any={value:4};const define:string='defineProperty';
console.log((Object as any)[define](target,key,{value:5,enumerable:true,configurable:true})===target,target[key]);
(Object as any)[define](target,'value',{get(){return 6;},configurable:true});console.log(target.value);
const definitions:string='defineProperties';(Object as any)[definitions](target,{other:{value:7,enumerable:true}});console.log(Object.keys(target).join(','),target.other);
const reflectMethod:string='preventExtensions';const reflected={value:8};console.log((Reflect as any)[reflectMethod](reflected),Object.isExtensible(reflected));
