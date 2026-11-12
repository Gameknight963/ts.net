export {};
for (const integrity of ['preventExtensions', 'seal', 'freeze']) {
    const prototype = Object.create(Array.prototype);
    prototype.marker = 7;
    const array: any = [1, 2];
    Object.setPrototypeOf(array, prototype);
    Object.defineProperty(array, 'extra', {get() {return 8;}, configurable: true});
    (Object as any)[integrity](array);
    console.log('array prototype', integrity, Object.getPrototypeOf(array) === prototype, array.marker, array.extra, array.join(','), Object.isExtensible(array));
    console.log('unchanged', Reflect.setPrototypeOf(array, prototype), Reflect.setPrototypeOf(array, Array.prototype));
    prototype.marker = 9;
    console.log('live prototype', array.marker);
}
for (const value of [new Number(3), new Date(0), /a/, new Map(), new Set(), function action() {}, Promise.resolve(1)]) {
    const original = Object.getPrototypeOf(value);
    const prototype = Object.create(original);
    prototype.marker = 5;
    Object.setPrototypeOf(value, prototype);
    console.log('custom', Object.getPrototypeOf(value) === prototype, (value as any).marker);
    Object.setPrototypeOf(value, null);
    console.log('null prototype', Object.getPrototypeOf(value) === null, (value as any).marker);
    Object.setPrototypeOf(value, original);
    console.log('restored', Object.getPrototypeOf(value) === original, (value as any).marker);
}
const prototype = {marker: 6};
const source = Object.setPrototypeOf({own: 4}, prototype);
const copy = {...source};
console.log('spread', Object.getPrototypeOf(copy) === Object.prototype, copy.own, (copy as any).marker, Object.getPrototypeOf(source) === prototype);

for (const integrity of ['preventExtensions', 'seal', 'freeze']) {
    const array: any = [3, 4];
    Object.setPrototypeOf(array, null);
    (Object as any)[integrity](array);
    console.log('null array prototype', integrity, Object.getPrototypeOf(array) === null, array[0], array.length);
    console.log('null prototype unchanged', Reflect.setPrototypeOf(array, null), Reflect.setPrototypeOf(array, Array.prototype));
}
