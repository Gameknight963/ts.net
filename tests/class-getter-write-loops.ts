export {};
class WriteHolder { base = 1; other = 2; get value(): number { return this.base * 2 + this.other; } }
function accumulate(target: WriteHolder, count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) {
        target.base = index;
        total += target.value;
    }
    return total;
}
const ordinary = new WriteHolder();
console.log('ordinary', accumulate(ordinary, 64), ordinary.base, ordinary.value);
console.log('second', accumulate(ordinary, 40), ordinary.base, ordinary.value);
const short = new WriteHolder();
console.log('short', accumulate(short, 1), short.base);
console.log('empty', accumulate(undefined as unknown as WriteHolder, 0), accumulate(undefined as unknown as WriteHolder, NaN));
const fractional = new WriteHolder();
console.log('fractional', accumulate(fractional, 32.5), fractional.base);
const sealed = Object.seal(new WriteHolder());
console.log('sealed', accumulate(sealed, 64), sealed.base, Object.isSealed(sealed));
const frozen = Object.freeze(new WriteHolder());
try { accumulate(frozen, 64); } catch (error) { console.log('frozen', (error as Error).name, frozen.base); }
const descriptor = new WriteHolder();
Object.defineProperty(descriptor, 'base', {value: 3, writable: true, configurable: false, enumerable: false});
console.log('descriptor', accumulate(descriptor, 64), descriptor.base, Object.keys(descriptor).join(','));
let storage = 7, writes = 0, reads = 0;
const accessor = new WriteHolder();
Object.defineProperty(accessor, 'base', {configurable: true, get() { reads++; return storage; }, set(value: number) { writes++; storage = value; }});
console.log('accessor', accumulate(accessor, 64), writes, reads, storage);
let observed = 0;
const observer = new WriteHolder();
Object.defineProperty(observer, 'other', {configurable: true, get() { observed += observer.base; return 3; }});
console.log('observer', accumulate(observer, 64), observed, observer.base);
const original = Object.getOwnPropertyDescriptor(WriteHolder.prototype, 'value')!;
try {
    Object.defineProperty(WriteHolder.prototype, 'value', {configurable: true, get() { observed += this.base; return this.base + 1; }});
    const replaced = new WriteHolder();
    console.log('replaced', accumulate(replaced, 64), observed, replaced.base);
} finally { Object.defineProperty(WriteHolder.prototype, 'value', original); }
class Derived extends WriteHolder { get value(): number { observed += this.base; return this.base + 2; } }
const derived = new Derived();
console.log('derived', accumulate(derived, 64), observed, derived.base);
const structural = {base: 0, other: 0, get value() { observed += this.base; return this.base; }};
console.log('structural', accumulate(structural, 64), observed, structural.base);
function distinct(write: WriteHolder, read: WriteHolder): number {
    let total = 0;
    for (let index = 0; index < 64; index++) { write.base = index; total += read.value; }
    return total;
}
const left = new WriteHolder(), right = new WriteHolder();
console.log('distinct', distinct(left, right), left.base, right.base);
function callbacks(target: WriteHolder): number {
    let total = 0;
    for (let index = 0; index < 64; index++) { target.base = index; observe(target); total += target.value; }
    return total;
}
function observe(target: WriteHolder) { observed += target.base; }
console.log('callbacks', callbacks(new WriteHolder()), observed);
class Literal { base = 1; get value(): number { return 5; } }
function constant(target: Literal): number {
    let total = 0;
    for (let index = 0; index < 64; index++) { target.base = index; total += target.value; }
    return total;
}
const literal = new Literal();
console.log('literal', constant(literal), literal.base);
class ZeroHolder { base = 1; get value(): number { return -0; } }
function zero(target: ZeroHolder): number {
    let total = -0;
    for (let index = 0; index < 64; index++) { target.base = index; total += target.value; }
    return total;
}
const zeroTarget = new ZeroHolder();
console.log('zero', Object.is(zero(zeroTarget), -0), zeroTarget.base);
const nonextensible = Object.preventExtensions(new WriteHolder());
console.log('nonextensible', accumulate(nonextensible, 64), nonextensible.base, Object.isExtensible(nonextensible));
function nonzero(target: WriteHolder): number {
    let total = 0;
    for (let index = 5; index < 69; index++) { target.base = index; total += target.value; }
    return total;
}
const started = new WriteHolder();
console.log('nonzero', nonzero(started), started.base);
const unchanged = new WriteHolder();
console.log('negative', accumulate(unchanged, -2), unchanged.base);
