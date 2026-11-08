export {};
let reads = 0, writes = 0;
const prototype = Object.create(null, {
    field: {get() {reads++; return 42;}, set() {writes++;}, configurable: true}
});
const value = Object.create(prototype);
Object.defineProperty(value, 'field', {value: 7, writable: false, configurable: true});
console.log('own', value.field, reads);
console.log('delete', Reflect.deleteProperty(value, 'field'), Object.hasOwn(value, 'field'));
console.log('inherited', value.field, reads);
value.field = 8;
console.log('setter', writes, Object.hasOwn(value, 'field'));
Object.defineProperty(value, 'field', {value: 9, enumerable: true, configurable: true});
console.log('recreated', value.field, Object.keys(value).join(','), reads, writes);
Object.defineProperty(value, 'fixed', {value: 1, writable: true});
console.log('fixed delete', Reflect.deleteProperty(value, 'fixed'), value.fixed);
value.fixed = 2;
console.log('fixed write', value.fixed, Object.getOwnPropertyDescriptor(value, 'fixed')!.configurable);
