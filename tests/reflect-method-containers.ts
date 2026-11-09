export {};
const methods={remove:Reflect.deleteProperty,define:Object.defineProperty,freeze:Object.freeze};
const target={storedDelete:1,keep:2};
console.log('stored delete',methods.remove(target,'storedDelete'),target.storedDelete,Object.keys(target).join(','));
const defined={storedDefinition:3};
methods.define(defined,'storedDefinition',{value:4,writable:false});
console.log('stored definition',defined.storedDefinition,Reflect.set(defined,'storedDefinition',5));
const frozen={storedFreeze:6};methods.freeze(frozen);
console.log('stored freeze',Object.isFrozen(frozen),Reflect.set(frozen,'storedFreeze',7));
function deletionMethod(){return Reflect.deleteProperty;}
const returned={returnedDelete:8};
console.log('returned delete',deletionMethod()(returned,'returnedDelete'),returned.returnedDelete);
const namespaces={reflection:Reflect,objects:Object};
const nested={nestedDelete:9};
console.log('namespace delete',namespaces.reflection.deleteProperty(nested,'nestedDelete'),nested.nestedDelete);
const nestedDefined={nestedDefinition:10};
namespaces.objects.defineProperty(nestedDefined,'nestedDefinition',{enumerable:false});
console.log('namespace definition',Object.keys(nestedDefined).length,nestedDefined.nestedDefinition);
let choose=false;
const alternatives={remove:choose ? Reflect.deleteProperty : (_target:object,_key:PropertyKey)=>false};
alternatives.remove=Reflect.deleteProperty;
const reassigned={assignedDelete:11};
console.log('assigned method',alternatives.remove(reassigned,'assignedDelete'),reassigned.assignedDelete);
const shorthandRemove=Reflect.deleteProperty;
const shorthand={shorthandRemove};const shortened={shortDelete:12};
console.log('shorthand',shorthand.shorthandRemove(shortened,'shortDelete'),shortened.shortDelete);
const indexedMethods=[Reflect.deleteProperty];const indexed={indexedDelete:13};
console.log('array method',indexedMethods[0](indexed,'indexedDelete'),indexed.indexedDelete);
function wrap<T>(value:T):T{return value;}
const wrapped={wrappedDelete:14};
console.log('generic return',wrap(Reflect.deleteProperty)(wrapped,'wrappedDelete'),wrapped.wrappedDelete);
const typedMethods:{remove:typeof Reflect.deleteProperty}={remove:Reflect.deleteProperty};
const typed={typedDelete:15};
console.log('typed container',typedMethods.remove(typed,'typedDelete'),typed.typedDelete);
const {remove:destructuredRemove}=methods;const destructured={destructuredDelete:16};
console.log('destructured property',destructuredRemove(destructured,'destructuredDelete'),destructured.destructuredDelete);
const [arrayRemove]=indexedMethods;const arrayDestructured={arrayBindingDelete:17};
console.log('destructured array',arrayRemove(arrayDestructured,'arrayBindingDelete'),arrayDestructured.arrayBindingDelete);
let reads=0;
const accessors={get remove(){reads++;return Reflect.deleteProperty;}};
const accessorTarget={accessorDelete:18};
console.log('getter method',accessors.remove(accessorTarget,'accessorDelete'),accessorTarget.accessorDelete,reads);
class MethodStore {remove=Reflect.deleteProperty;}
const classTarget={classStoredDelete:19};
console.log('class field',new MethodStore().remove(classTarget,'classStoredDelete'),classTarget.classStoredDelete);
const methodName='remove';
const computedMethods:{remove:typeof Reflect.deleteProperty}={[methodName]:Reflect.deleteProperty};
const computed={computedAliasDelete:20};
console.log('computed method',computedMethods[methodName](computed,'computedAliasDelete'),computed.computedAliasDelete);
const {[methodName]:computedRemove}=computedMethods;const computedBinding={computedBindingDelete:21};
console.log('computed binding',computedRemove(computedBinding,'computedBindingDelete'),computedBinding.computedBindingDelete);
function definitionMethod(){return Object.defineProperty;}
const factoryDefined={factoryDefinition:22};
definitionMethod()(factoryDefined,'factoryDefinition',{value:23,enumerable:false});
console.log('returned definition',factoryDefined.factoryDefinition,Object.keys(factoryDefined).length);
function freezeMethod(){return Object.freeze;}
const factoryFrozen={factoryFreeze:24};freezeMethod()(factoryFrozen);
console.log('returned freeze',Object.isFrozen(factoryFrozen),Reflect.set(factoryFrozen,'factoryFreeze',25));
