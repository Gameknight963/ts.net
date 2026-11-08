export {};
class Example {
    value = 1;
    get doubled() {return this.value * 2;}
    method() {return this.value;}
}
const instance = new Example();
Object.defineProperty(instance, 'value', {value: 3, writable: false});
console.log('value', instance.value, instance.doubled, instance.method());
try {instance.value = 4;} catch (error) {console.log('readonly', (error as Error).name);}
Object.defineProperty(instance, 'extra', {get() {return this.value + 1;}, enumerable: true, configurable: true});
console.log('extra', (instance as any).extra, Object.keys(instance).join(','));
Object.defineProperty(Example.prototype, 'method', {enumerable: true});
console.log('prototype', Object.keys(Example.prototype).join(','), instance.method());
Object.freeze(instance);
console.log('frozen', Object.isFrozen(instance), Reflect.defineProperty(instance, 'extra', {value: 9}), Reflect.set(instance, 'newField', 2));
console.log('delete', Reflect.deleteProperty(instance, 'value'));
const sealed = Object.seal(new Example());
sealed.value = 5;
console.log('sealed', sealed.value, Object.isSealed(sealed), Object.isFrozen(sealed));
Object.defineProperty(Example.prototype, 'inherited', {value: 7, writable: false});
const child = new Example();
try {(child as any).inherited = 8;} catch (error) {console.log('inherited readonly', (error as Error).name);}
console.log('inherited', (child as any).inherited, Object.hasOwn(child, 'inherited'));
const hidden = new Example();
Object.defineProperty(hidden, 'secret', {value: 10, writable: true, configurable: true});
console.log('hidden names', Object.getOwnPropertyNames(hidden).join(','), Object.keys(hidden).join(','));
Object.freeze(hidden);
console.log('hidden frozen', Object.getOwnPropertyDescriptor(hidden, 'secret')!.writable, Object.getOwnPropertyDescriptor(hidden, 'secret')!.configurable);
try {(hidden as any).secret = 11;} catch (error) {console.log('hidden readonly', (error as Error).name);}
console.log('prototype names', Object.getOwnPropertyNames(Example.prototype).join(','));
