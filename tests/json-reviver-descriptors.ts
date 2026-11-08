export {};
const frozen = JSON.parse('{"a":1,"b":2}', function(this: any, key: string, value: any) {
    if (key === 'a') Object.freeze(this);
    return typeof value === 'number' ? value + 10 : value;
});
console.log('frozen', JSON.stringify(frozen), Object.isFrozen(frozen));
const closed = JSON.parse('{"a":1,"b":2}', function(this: any, key: string, value: any) {
    if (key === 'a') {delete this.b; Object.preventExtensions(this);}
    return key === 'a' ? 5 : key === 'b' ? 6 : value;
});
console.log('closed', JSON.stringify(closed), Object.hasOwn(closed, 'b'), Object.isExtensible(closed));
let setterCalls = 0;
const accessor = JSON.parse('{"a":1,"b":2}', function(this: any, key: string, value: any) {
    if (key === 'a') Object.defineProperty(this, 'b', {get() {return 7;}, set() {setterCalls++;}, enumerable: false, configurable: true});
    return key === 'b' ? value + 1 : value;
});
console.log('accessor replaced', JSON.stringify(accessor), setterCalls, JSON.stringify(Object.getOwnPropertyDescriptor(accessor, 'b')));
const readonly = JSON.parse('{"a":1,"b":2}', function(this: any, key: string, value: any) {
    if (key === 'a') Object.defineProperty(this, 'b', {writable: false, configurable: true});
    return key === 'b' ? 9 : value;
});
console.log('readonly replaced', JSON.stringify(readonly), JSON.stringify(Object.getOwnPropertyDescriptor(readonly, 'b')));
let arraySetters = 0;
const array = JSON.parse('[1,2]', function(this: any, key: string, value: any) {
    if (key === '0') Object.defineProperty(this, '1', {get() {return 7;}, set() {arraySetters++;}, configurable: true});
    return key === '1' ? value + 1 : value;
});
console.log('array accessor replaced', JSON.stringify(array), arraySetters, JSON.stringify(Object.getOwnPropertyDescriptor(array, '1')));
const frozenArray = JSON.parse('[1,2]', function(this: any, key: string, value: any) {
    if (key === '0') Object.freeze(this);
    return typeof value === 'number' ? value + 10 : value;
});
console.log('frozen array', JSON.stringify(frozenArray), Object.isFrozen(frozenArray));
