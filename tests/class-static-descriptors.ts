export {};
class Base {
    static value = 1;
    static method() {return this.value;}
}
Object.defineProperty(Base, 'value', {writable: false});
try {Base.value = 2;} catch (error) {console.log('readonly', (error as Error).name);}
Object.defineProperty(Base, 'method', {enumerable: true});
console.log('keys', Object.keys(Base).join(','));
Object.defineProperty(Base, 'extra', {get() {return this.value + 3;}, configurable: true});
console.log('accessor', (Base as any).extra, Base.method());
Object.defineProperty(Base, 'name', {value: 'Renamed', writable: true});
(Base as any).name = 'Changed';
console.log('metadata', Base.name, Object.getOwnPropertyDescriptor(Base, 'name')!.enumerable);
class Child extends Base {}
try {Child.value = 4;} catch (error) {console.log('inherited readonly', (error as Error).name);}
console.log('inherited', Child.value, Object.hasOwn(Child, 'value'));
Object.freeze(Base);
console.log('freeze', Object.isFrozen(Base), Reflect.defineProperty(Base, 'value', {value: 9}), Reflect.set(Base, 'newField', 1));
console.log('prototype', Reflect.defineProperty(Base, 'prototype', {value: Base.prototype}), Reflect.defineProperty(Base, 'prototype', {value: {}}));
console.log('names', Object.getOwnPropertyNames(Base).join(','));
console.log('delete prototype', Reflect.deleteProperty(Child, 'prototype'), Object.hasOwn(Child, 'prototype'));
