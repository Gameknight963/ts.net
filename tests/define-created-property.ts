export {};
const value = Object.create(null);
console.log('identity', Object.defineProperty(value, 'a', {value: 1, writable: true, configurable: true}) === value);
Object.defineProperty(value, 'a', {enumerable: true});
console.log('preserved', value.a, JSON.stringify(Object.getOwnPropertyDescriptor(value, 'a')));
let stored = 7;
function read(): number {return stored;}
Object.defineProperty(value, 'a', {get: read, set(next: number) {stored = next;}});
value.a = 8;
console.log('accessor', value.a, read.name, Object.keys(value).join(','));
Object.defineProperty(value, 'a', {value: 9});
console.log('data', value.a, JSON.stringify(Object.getOwnPropertyDescriptor(value, 'a')));
Object.defineProperty(value, 'fixed', {value: NaN});
Object.defineProperty(value, 'fixed', {value: NaN});
for (const descriptor of [{value: 1}, {writable: true}, {configurable: true}, {enumerable: true}, {get: read}]) {
    try {Object.defineProperty(value, 'fixed', descriptor);} catch (error) {console.log('invalid', (error as Error).name);}
}
Object.defineProperty(value, 'zero', {value: -0});
try {Object.defineProperty(value, 'zero', {value: 0});} catch (error) {console.log('zero', (error as Error).name);}
Object.defineProperty(value, 'a', {});
console.log('order', Object.getOwnPropertyNames(value).join(','));
const accessor = Object.create(null, {item: {get: read}});
console.log('same getter', Reflect.defineProperty(accessor, 'item', {get: read}));
console.log('different getter', Reflect.defineProperty(accessor, 'item', {get() {return 10;}}));
console.log('add setter', Reflect.defineProperty(accessor, 'item', {set(next: number) {stored = next;}}));
console.log('remove getter', Reflect.defineProperty(accessor, 'item', {get: undefined}));
console.log('empty accessor', Reflect.defineProperty(accessor, 'item', {}), accessor.item);
const changing = Object.create(null, {item: {value: 1, writable: true, enumerable: true, configurable: true}});
Object.defineProperty(changing, 'item', {get: read});
let descriptor = Object.getOwnPropertyDescriptor(changing, 'item')!;
console.log('to accessor', descriptor.get === read, descriptor.set === undefined, 'value' in descriptor, descriptor.enumerable, descriptor.configurable);
Object.defineProperty(changing, 'item', {writable: true});
descriptor = Object.getOwnPropertyDescriptor(changing, 'item')!;
console.log('to data', descriptor.value === undefined, descriptor.writable, 'get' in descriptor, descriptor.enumerable, descriptor.configurable);
