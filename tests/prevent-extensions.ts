export {};
const value: any = {field: 1};
const prototype = Object.getPrototypeOf(value);
console.log('identity', Object.preventExtensions(value) === value, Object.isExtensible(value), Reflect.isExtensible(value));
value.field = 2;
console.log('existing', value.field, Reflect.set(value, 'field', 3), value.field);
try {value.added = 4;} catch (error) {console.log('add', (error as Error).name);}
console.log('reflect add', Reflect.set(value, 'added', 4), Reflect.defineProperty(value, 'added', {value: 4}));
try {Object.defineProperty(value, 'added', {value: 4});} catch (error) {console.log('define', (error as Error).name);}
console.log('prototype', Reflect.setPrototypeOf(value, prototype), Reflect.setPrototypeOf(value, null));
try {Object.setPrototypeOf(value, null);} catch (error) {console.log('prototype error', (error as Error).name);}
console.log('delete', Reflect.deleteProperty(value, 'field'));
console.log('integrity', Object.isSealed(value), Object.isFrozen(value), Object.keys(value).length);
console.log('repeat', Reflect.preventExtensions(value));
const parent = {set field(next: number) {console.log('setter', next);}};
const child = Object.create(parent);
Object.preventExtensions(child);
child.field = 7;
console.log('inherited', Object.hasOwn(child, 'field'));
for (const primitive of [null, undefined, 1, 'text', Symbol('value')]) {
    console.log('primitive', Object.preventExtensions(primitive) === primitive);
    try {Reflect.preventExtensions(primitive as any);} catch (error) {console.log('reflect primitive', (error as Error).name);}
}
