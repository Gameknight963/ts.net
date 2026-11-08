export {};
const prototype = BigInt.prototype;
const boxed = Object(123n);
console.log('boxed', prototype.valueOf.call(boxed), prototype.toString.call(boxed, 16));
console.log('parent', Object.getPrototypeOf(prototype) === Object.prototype);
console.log('tag', Object.prototype.toString.call(prototype), Object.prototype.toString.call(boxed));
for (const receiver of [prototype, {}, null, undefined, 1, '1']) {
    for (const method of [prototype.valueOf, prototype.toString]) {
        try { (method as Function).call(receiver); }
        catch (error) { console.log('receiver', (error as Error).name); }
    }
}
for (const key of ['constructor', 'valueOf', 'toString', 'toLocaleString', Symbol.toStringTag]) {
    const descriptor = Object.getOwnPropertyDescriptor(prototype, key)!;
    console.log('descriptor', String(key), descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
try { (prototype as any)[Symbol.toStringTag] = 'changed'; }
catch (error) { console.log('readonly', (error as Error).name); }
const original = prototype.valueOf;
try {
    prototype.valueOf = function () { return 456n; };
    console.log('override', Number(boxed), BigInt(boxed), String(boxed));
    delete (prototype as any)[Symbol.toStringTag];
    console.log('deleted tag', Object.prototype.toString.call(boxed));
} finally { prototype.valueOf = original; }
