export {};
function createCallbackInput(count: number): any {
    const values = new Float64Array(count);
    for (let i = 0; i < count; i++) values[i] = i;
    return values;
}
function predicateKernel(values: Float64Array): number {
    let total = 0;
    if (values.every(value => value >= 0)) total += 7;
    if (values.some(value => value < 0)) total++;
    return total + values.findIndex(value => value < 0);
}
function reduceKernel(values: Float64Array): number { return values.reduce((sum, value) => sum + value, 0); }
console.log('kernels', predicateKernel(createCallbackInput(4)), reduceKernel(createCallbackInput(4)));
const input = new Float64Array([1, 2, 3, 4]);
let calls = 0;
console.log('early', input.every(value => { calls++; return value < 2; }), calls);
calls = 0;
console.log('some', input.some(value => { calls++; return value === 2; }), calls);
console.log('numeric', input.every(((value: number) => value) as any), input.some((() => NaN) as any), input.findIndex(((value: number) => value - 1) as any));
console.log('reduce', input.reduce((sum, value) => sum + value, 0), input.reduceRight((sum, value) => sum - value));
console.log('defaults', new Float64Array([1, 2]).reduce((sum = 7, value) => sum + value, undefined as unknown as number));
let valueDefaults = 0, detached = false;
const defaultBuffer = new ArrayBuffer(24), defaultValues = new Float64Array(defaultBuffer);
defaultValues.set([1, 2, 3]);
const defaultSum = defaultValues.reduce((sum, value = ++valueDefaults) => {
    if (!detached) { detached = true; defaultBuffer.transfer(); }
    return sum + value;
}, 0);
console.log('detached defaults', defaultSum, valueDefaults);
console.log('missing state', Number.isNaN(input.reduce((sum, value) => value === 2 ? undefined as unknown as number : sum + value, 0)));
console.log('missing result', typeof input.reduce(() => undefined as unknown as number, 0));
console.log('empty initial', (new Float64Array() as any).reduce((sum: number, value: number) => sum + value, 'seed'));
try { new Float64Array().reduce((sum, value) => sum + value); } catch (error) { console.log('empty', (error as Error).name); }
console.log('zero', Object.is(new Float64Array([0, -0]).find(value => Object.is(value, -0)), -0));
const mutable = new Float64Array([1, 2, 3, 4]);
console.log('snapshot', mutable.find((value, index) => { mutable[index] = 99; return true; }), mutable.findLast((value, index) => { mutable[index] = 99; return true; }));
console.log('mutated', mutable.join(','));
function receiver(this: { limit: number }, value: number): boolean { return value > this.limit; }
console.log('receiver', input.some(receiver, { limit: 2 }));
function argumentsCount(value: number): boolean { return arguments.length === 3 && value > 1; }
console.log('arguments', input.findIndex(argumentsCount));
function reductionArguments(sum: number, value: number): number { return sum + value + arguments.length; }
console.log('reduction arguments', input.reduce(reductionArguments, 0));
let total = 0;
input.forEach((value, index, array) => { total += value + index + array.length; });
console.log('source', total);
const readers: (() => number)[] = [];
input.forEach(value => { let local = value; readers.push(() => ++local); });
console.log('activations', readers[0](), readers[0](), readers[1]());
console.log('bigint', new BigInt64Array([1n, 2n]).every(value => value > 0n), new BigInt64Array([1n, 2n]).reduce((sum, value) => sum + value, 0n));
for (const Type of [Int8Array, Uint8Array, Uint8ClampedArray, Int16Array, Uint16Array, Int32Array, Uint32Array, Float16Array, Float32Array, Float64Array] as any[]) {
    const values = new Type([1, 2, 3]);
    console.log(Type.name, values.every((value: number, index: number) => value > index), values.reduce((sum: number, value: number) => sum + value, 0));
}
const edges: [any, number[]][] = [
    [Int8Array, [-128, -1, 0, 127]], [Uint8Array, [0, 127, 128, 255]],
    [Uint8ClampedArray, [-1, 0.5, 1.5, 254.5, 255.5, NaN]],
    [Int16Array, [-32768, -1, 0, 32767]], [Uint16Array, [0, 32767, 32768, 65535]],
    [Int32Array, [-2147483648, -1, 0, 2147483647]], [Uint32Array, [0, 2147483647, 2147483648, 4294967295]],
    [Float16Array, [-0, -65504, 1 / 3, Infinity, NaN]],
    [Float32Array, [-0, -3.4e38, 1 / 3, Infinity, NaN]],
    [Float64Array, [-0, Number.MIN_VALUE, Number.MAX_VALUE, Infinity, NaN]]
];
for (const [Type, values] of edges) {
    const buffer = new ArrayBuffer((values.length + 2) * Type.BYTES_PER_ELEMENT);
    const view = new Type(buffer, Type.BYTES_PER_ELEMENT, values.length);
    view.set(values);
    console.log('edges', Type.name, Array.from(view).join(','), view[1], Object.is(view[0], -0));
}
const shared = new SharedArrayBuffer(16, { maxByteLength: 32 });
const sharedValues = new Float64Array(shared);
sharedValues.set([1, 2]);
let sharedCalls = 0;
console.log('shared', sharedValues.every(value => { if (++sharedCalls === 1) shared.grow(32); return value > 0; }), sharedCalls, sharedValues.length);
let visited = 0;
try { input.forEach(value => { visited++; if (value === 2) throw new Error('callback'); }); }
catch (error) { console.log('throw', (error as Error).message, visited); }
for (const tracking of [false, true]) for (const mode of ['shrink', 'grow', 'detach']) {
    for (const method of ['forEach', 'every', 'some', 'find', 'findIndex', 'findLast', 'findLastIndex']) {
        const buffer = new ArrayBuffer(32, { maxByteLength: 64 });
        const values = tracking ? new Float64Array(buffer) : new Float64Array(buffer, 0, 4);
        values.set([1, 2, 3, 4]);
        const seen: string[] = [];
        const result = (values as any)[method]((value: number, index: number, source: Float64Array) => {
            seen.push(index + ':' + String(value) + ':' + source.length);
            if (seen.length === 1) {
                if (mode === 'shrink') buffer.resize(16);
                if (mode === 'grow') buffer.resize(48);
                if (mode === 'detach') buffer.transfer();
            }
            return method === 'every';
        });
        console.log(tracking, mode, method, result, seen.join('|'));
    }
    for (const method of ['reduce', 'reduceRight']) {
        const buffer = new ArrayBuffer(32, { maxByteLength: 64 });
        const values = tracking ? new Float64Array(buffer) : new Float64Array(buffer, 0, 4);
        values.set([1, 2, 3, 4]);
        const seen: string[] = [];
        const result = (values as any)[method]((sum: number, value: number) => {
            seen.push(String(value));
            if (seen.length === 1) {
                if (mode === 'shrink') buffer.resize(16);
                if (mode === 'grow') buffer.resize(48);
                if (mode === 'detach') buffer.transfer();
            }
            return sum + (value === undefined ? 0 : value);
        }, 0);
        console.log(tracking, mode, method, result, seen.join('|'));
    }
}
