/// <reference lib="es2024.arraybuffer" />
export {};
for (const action of ['shrink', 'grow', 'detach']) {
    const source = new ArrayBuffer(4, {maxByteLength: 8});
    new Uint8Array(source).set([1, 2, 3, 4]);
    let requested = -1;
    Object.defineProperty(source, 'constructor', {value: {[Symbol.species]: function(length: number) {
        requested = length;
        const result = new ArrayBuffer(length + 1);
        new Uint8Array(result).fill(9);
        if (action === 'detach') source.transfer(); else source.resize(action === 'shrink' ? 1 : 8);
        return result;
    }}});
    try {const result = source.slice(0); console.log('result', action, requested, new Uint8Array(result).join(','));}
    catch (error) {console.log('error', action, requested, (error as Error).name);}
}
const source = new ArrayBuffer(4);
let called = 0;
Object.defineProperty(source, 'constructor', {value: {[Symbol.species]: function(length: number) {called++; return new ArrayBuffer(length);}}});
try {source.slice({valueOf() {source.transfer(); return 0;}} as any, 0);}
catch (error) {console.log('empty detached', called, (error as Error).name);}
const target = new ArrayBuffer(4);
target.transfer();
const other = new ArrayBuffer(4);
Object.defineProperty(other, 'constructor', {value: {[Symbol.species]: function() {return target;}}});
try {other.slice(0, 0);} catch (error) {console.log('detached result', (error as Error).name);}
