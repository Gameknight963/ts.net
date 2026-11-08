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
