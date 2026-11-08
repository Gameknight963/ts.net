export {};
let configurable = false, calls = 0;
function Base(): any {
    const value = Object.fromEntries([['field', 1]]);
    Object.defineProperty(value, 'field', {writable: false, enumerable: false, configurable});
    return value;
}
class Child extends (Base as any) {field = 7;}
try {new Child();} catch (error) {console.log('fixed', (error as Error).name);}
configurable = true;
const child = new Child();
console.log('replace', child.field, JSON.stringify(Object.getOwnPropertyDescriptor(child, 'field')));
function AccessorBase(): any {
    const value = Object.create(null);
    Object.defineProperty(value, 'field', {get() {return 1;}, set() {calls++;}, configurable: true});
    return value;
}
class AccessorChild extends (AccessorBase as any) {field = 9;}
const accessor = new AccessorChild();
console.log('accessor', accessor.field, calls, JSON.stringify(Object.getOwnPropertyDescriptor(accessor, 'field')));
