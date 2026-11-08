/// <reference lib="es2023.array" />
/// <reference lib="es2024.arraybuffer" />
export {};
for (const action of ['shrink', 'grow', 'detach']) {
    const buffer = new ArrayBuffer(3, {maxByteLength: 6}), source = new Uint8Array(buffer);
    source.set([3, 1, 2]);
    let changed = false;
    const sorted = source.toSorted((a, b) => {
        if (!changed) {changed = true; if (action === 'detach') buffer.transfer(); else buffer.resize(action === 'shrink' ? 1 : 6);}
        return a - b;
    });
    console.log('copied', action, sorted.length, sorted.join(','), source.length);
}
for (const action of ['shrink', 'detach']) {
    const buffer = new ArrayBuffer(3, {maxByteLength: 6}), source = new Uint8Array(buffer);
    source.set([3, 1, 2]);
    let changed = false;
    const result = source.sort((a, b) => {if (!changed) {changed = true; if (action === 'detach') buffer.transfer(); else buffer.resize(1);} return a - b;});
    console.log('in place', action, result === source, source.length, source[0]);
}
const big = new BigInt64Array([3n, 1n, 2n]);
console.log('bigint', big.toSorted((a, b) => a < b ? -1 : a > b ? 1 : 0).join(','), big.join(','));
const values = new Uint8Array([3, 1, 2]);
Object.defineProperty(values, 'constructor', {get() {throw new Error('species must not run');}});
Object.defineProperty(values, 'length', {get() {throw new Error('public length');}});
console.log('internal', values.toSorted().join(','));
try {values.toSorted(() => {values[0] = 9; throw new Error('compare');});}
catch (error) {console.log('throw', (error as Error).message, values.join(','));}
const floating = new Float64Array([NaN, 0, -0, 1, -1]).toSorted();
console.log('floating', floating[0], Object.is(floating[1], -0), Object.is(floating[2], 0), floating[3], Number.isNaN(floating[4]));
