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
class InheritedBase {}
Object.defineProperty(InheritedBase.prototype, 'field', {value: 2, writable: false});
class InheritedChild extends InheritedBase {field = 12;}
const inherited = new InheritedChild();
console.log('inherited field', inherited.field, JSON.stringify(Object.getOwnPropertyDescriptor(inherited, 'field')));
let setterCalls = 0;
class SetterBase {}
Object.defineProperty(SetterBase.prototype, 'field', {set(value: number) {setterCalls++;}});
class SetterChild extends SetterBase {field = 13;}
console.log('field skips setter', new SetterChild().field, setterCalls);
class FixedBase {
    constructor() {Object.defineProperty(this, 'field', {value: 1, configurable: false});}
}
class FixedChild extends FixedBase {field = 14;}
try {new FixedChild();} catch (error) {console.log('class fixed', (error as Error).name);}
class ClosedBase {
    constructor() {Object.preventExtensions(this);}
}
class ClosedChild extends ClosedBase {field = 15;}
try {new ClosedChild();} catch (error) {console.log('class closed', (error as Error).name);}
class MutableBase {
    constructor() {Object.defineProperty(this, 'field', {get() {return 1;}, configurable: true});}
}
class MutableChild extends MutableBase {field = 16;}
const mutable = new MutableChild();
console.log('class replacement', mutable.field, JSON.stringify(Object.getOwnPropertyDescriptor(mutable, 'field')));
