/// <reference lib="es2024.arraybuffer" />
export {};
function Alternate() {}
let order = '', action = '';
let buffer = new ArrayBuffer(8, {maxByteLength: 16});
const bound = Alternate.bind(null);
Object.defineProperty(bound, 'prototype', {get() {order += 'prototype|'; if (action === 'detach') buffer.transfer(); else if (action === 'shrink') buffer.resize(2); return Alternate.prototype;}});
for (const next of ['', 'detach', 'shrink']) {
    action = next; order = ''; buffer = new ArrayBuffer(8, {maxByteLength: 16});
    const offset = {valueOf() {order += 'offset|'; return 1;}};
    const length = {valueOf() {order += 'length|'; return 4;}};
    try {const view = Reflect.construct(DataView, [buffer, offset, length], bound); console.log('view', next, order, DataView.prototype.getUint8.call(view, 0), Object.getPrototypeOf(view) === Alternate.prototype);}
    catch (error) {console.log('error', next, order, (error as Error).name);}
}
action = ''; order = ''; buffer = new ArrayBuffer(8);
try {Reflect.construct(DataView, [buffer, 9], bound);} catch (error) {console.log('bounds', order, (error as Error).name);}
order = '';
try {Reflect.construct(DataView, [{}, 0], bound);} catch (error) {console.log('buffer', order, (error as Error).name);}
action = 'shrink'; order = ''; buffer = new ArrayBuffer(8, {maxByteLength: 16});
try {const view = Reflect.construct(DataView, [buffer, 1], bound); const getLength = Object.getOwnPropertyDescriptor(DataView.prototype, 'byteLength')!.get!; console.log('tracking', order, getLength.call(view));}
catch (error) {console.log('tracking error', order, (error as Error).name);}
for (const change of ['grow', 'shrink', 'detach']) {
    action = ''; order = ''; buffer = new ArrayBuffer(8, {maxByteLength: 16});
    const length = {valueOf() {order += 'length|'; if (change === 'detach') buffer.transfer(); else buffer.resize(change === 'grow' ? 16 : 2); return change === 'grow' ? 12 : 4;}};
    try {Reflect.construct(DataView, [buffer, 0, length], bound); console.log('length accepted', change);}
    catch (error) {console.log('length conversion', change, order, (error as Error).name);}
}
