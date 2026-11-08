export {};
const object: any = {};
const get = function(this: any) {return this.stored ?? 3;};
const set = function(this: any, value: number) {this.stored = value;};
console.log('result', object.__defineGetter__('value', get) === undefined, object.__defineSetter__('value', set) === undefined);
object.value = 8;
const descriptor = Object.getOwnPropertyDescriptor(object, 'value')!;
console.log('accessor', object.value, descriptor.get === get, descriptor.set === set, descriptor.enumerable, descriptor.configurable, object.__lookupGetter__('value') === get, object.__lookupSetter__('value') === set);
const key = Symbol('key');
object.__defineGetter__(key, () => 42);
console.log('symbol', object[key]);
const array: any = [1];
(Object.prototype as any).__defineGetter__.call(array, '0', () => 9);
console.log('array', array[0], array.length);
let conversions = 0;
try {object.__defineGetter__({toString() {conversions++; return 'bad';}}, 3);} catch (error) {console.log('invalid', (error as Error).name, conversions);}
Object.preventExtensions(object);
try {object.__defineGetter__('new', get);} catch (error) {console.log('closed', (error as Error).name);}
for (const name of ['__defineGetter__', '__defineSetter__']) {
    const method = (Object.prototype as any)[name];
    const descriptor = Object.getOwnPropertyDescriptor(Object.prototype, name)!;
    console.log('method', method.name, method.length, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    Reflect.deleteProperty(Object.prototype, name);
    console.log('deleted', name in {});
    Object.defineProperty(Object.prototype, name, descriptor);
}
for (const receiver of [null, undefined]) {
    try {(Object.prototype as any).__defineGetter__.call(receiver, 'x', get);}
    catch (error) {console.log('receiver', (error as Error).name);}
}
const fixed: any = Object.defineProperty({}, 'x', {value: 1});
try {fixed.__defineSetter__('x', set);} catch (error) {console.log('fixed', (error as Error).name, fixed.x);}
