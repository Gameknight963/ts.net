export {};
function Alternate() {}
const bound = Alternate.bind(null);
let order = '';
Object.defineProperty(bound, 'prototype', {get() {order += 'prototype|'; return Alternate.prototype;}});
for (const constructor of [Date, Number, String, ArrayBuffer, SharedArrayBuffer, Uint8Array]) {
    order = '';
    const input = {valueOf() {order += 'value|'; return 1;}, toString() {order += 'string|'; return '1';}};
    const value = Reflect.construct(constructor, [input], bound);
    console.log('conversion', constructor.name, order, Object.getPrototypeOf(value) === Alternate.prototype);
}
for (const constructor of [Date, Number, String]) {
    order = '';
    const input = {[Symbol.toPrimitive]() {order += 'conversion|'; throw new Error('stop');}};
    try {Reflect.construct(constructor, [input], bound);} catch (error) {console.log('failure', constructor.name, order, (error as Error).message);}
}
for (const constructor of [ArrayBuffer, SharedArrayBuffer]) {
    order = '';
    const options = {get maxByteLength() {order += 'options|'; return {valueOf() {order += 'maximum|'; return 8;}};}};
    const value = Reflect.construct(constructor, [1, options], bound);
    console.log('options', constructor.name, order, Object.getPrototypeOf(value) === Alternate.prototype);
    order = '';
    try {Reflect.construct(constructor, [-1], bound);} catch (error) {console.log('length', constructor.name, order, (error as Error).name);}
}
