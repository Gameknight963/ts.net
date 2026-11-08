export {};
const methods = ['map', 'push', 'fill', 'toSorted', 'values'];
for (const key of methods) {
    const descriptor = Object.getOwnPropertyDescriptor(Array.prototype, key)!;
    console.log('before', key, key in [], typeof ([] as any)[key]);
    Reflect.deleteProperty(Array.prototype, key);
    console.log('deleted', key, key in [], key in Array.prototype, typeof ([] as any)[key]);
    Object.defineProperty(Array.prototype, key, descriptor);
    console.log('restored', key, key in []);
}
const detached = [1, 2];
Object.setPrototypeOf(detached, null);
for (const key of methods) console.log('no prototype', key, key in detached);
