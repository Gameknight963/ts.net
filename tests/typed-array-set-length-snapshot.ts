/// <reference lib="es2024.arraybuffer" />
export {};
for (const action of ['grow', 'shrink', 'detach']) {
    const buffer = new ArrayBuffer(3, {maxByteLength: 6}), target = new Uint8Array(buffer);
    let order = '';
    const source = {get length() {order += 'length|'; if (action === 'detach') buffer.transfer(); else buffer.resize(action === 'grow' ? 6 : 1); return action === 'grow' ? 4 : 3;},
        get 0() {order += '0|'; return 1;}, get 1() {order += '1|'; return 2;}, get 2() {order += '2|'; return 3;}, get 3() {order += '3|'; return 4;}};
    try {target.set(source); console.log('result', action, order, target.length, target[0], target[1], target[2]);}
    catch (error) {console.log('error', action, order, (error as Error).name);}
}
const buffer = new ArrayBuffer(3, {maxByteLength: 6}), target = new Uint8Array(buffer);
let order = '';
const offset = {valueOf() {order += 'offset|'; buffer.resize(6); return 1;}};
const source = {get length() {order += 'length|'; return 3;}, 0: 7, 1: 8, 2: 9};
target.set(source, offset as any);
console.log('offset', order, target.join(','));
const changing = new Uint8Array(3);
order = '';
const elements = {length: 3,
    get 0() {order += '0|'; changing.buffer.transfer(); return {valueOf() {order += 'value0|'; return 1;}};},
    get 1() {order += '1|'; return {valueOf() {order += 'value1|'; throw new Error('conversion');}};},
    get 2() {order += '2|'; return 3;}};
try {changing.set(elements as any);} catch (error) {console.log('element conversions', order, (error as Error).message);}
