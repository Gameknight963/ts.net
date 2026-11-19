export {};

class Flags {
    first = false;
    second = true;
    third: boolean = undefined as any;
    value = 7;
    constructor(first: boolean, third: boolean) {
        this.first = first;
        this.third = third;
    }
}
function fresh(): Flags { return new Flags(false, true); }
function typed(flags: Flags): string {
    return `${flags.first}:${flags.second}:${flags.third}:${flags.value}`;
}
for (const value of [true, false, undefined]) {
    const flags = new Flags(value as any, value as any);
    console.log('constructed', typed(flags), Object.hasOwn(flags, 'first'), Object.hasOwn(flags, 'third'));
    console.log('json', JSON.stringify(flags));
}

const changing = fresh();
for (const value of [true, false, undefined, false, true]) {
    changing.first = value as any;
    console.log('changing', typed(changing), Object.hasOwn(changing, 'first'));
}

// Each incompatible write starts with a fresh layout, before key enumeration
// or a descriptor operation has moved it into the ordinary property store.
for (const value of [null, 0, 1, '', 'text', {tag: 3}, [false, true]]) {
    const flags: any = fresh();
    flags.first = value;
    console.log('dynamic', typeof flags.first, JSON.stringify(flags.first), flags.second, flags.third, flags.value);
    flags.first = false;
    console.log('restored', typed(flags), Object.keys(flags).join(','));
}
for (const value of [Symbol.for('boolean field'), 2n, () => 3]) {
    const flags: any = fresh();
    flags.first = value;
    console.log('opaque', typeof flags.first, flags.first === value, flags.second, flags.third);
    flags.first = undefined;
    console.log('opaque clear', flags.first, Object.hasOwn(flags, 'first'), JSON.stringify(flags));
}
const text = fresh();
(text as unknown as {first: string}).first = 'text';
console.log('string view', (text as any).first, text.second, text.third);
const number = fresh();
(number as unknown as {first: number}).first = 9;
console.log('number view', (number as any).first, number.second, number.third);

let initializer: any;
class Initialized { active: boolean = initializer; other = true; }
for (initializer of [true, false, undefined, null, 1, 'text']) {
    const flags: any = new Initialized();
    console.log('initializer', typeof flags.active, JSON.stringify(flags), Object.hasOwn(flags, 'active'));
}
class Parameters {
    constructor(public active: boolean, public complete: boolean = false) {}
}
for (const value of [true, false, undefined]) {
    const flags = new Parameters(value as any);
    console.log('parameters', flags.active, flags.complete, Object.keys(flags).join(','));
}

const deleted = fresh();
delete (deleted as any).first;
console.log('deleted', deleted.first, Object.hasOwn(deleted, 'first'), deleted.second, deleted.third);
Object.defineProperty(Flags.prototype, 'first', {get() { return true; }, configurable: true});
console.log('inherited', deleted.first);
delete (Flags.prototype as any).first;
deleted.first = false;
console.log('recreated', typed(deleted), Object.keys(deleted).join(','));

const described = fresh();
Object.defineProperty(described, 'first', {value: true, writable: false});
try { described.first = false; } catch { console.log('readonly'); }
console.log('descriptor', typed(described), Object.getOwnPropertyDescriptor(described, 'first')!.value);
const accessor = fresh();
let writes = 0;
Object.defineProperty(accessor, 'first', {get() { return false; }, set(value: boolean) { writes += value ? 1 : 2; }, configurable: true});
accessor.first = true;
console.log('accessor', typed(accessor), writes);
const frozen = fresh();
Object.freeze(frozen);
try { frozen.first = true; } catch { console.log('frozen'); }
console.log('frozen value', typed(frozen));
const sealed = fresh();
Object.seal(sealed);
sealed.first = true;
console.log('sealed', typed(sealed), Reflect.deleteProperty(sealed, 'first'));

const incremented: any = fresh();
incremented.first = true;
console.log('increment', incremented.first++, incremented.first, incremented.second, incremented.third);
const concatenated: any = fresh();
concatenated.first += 'x';
console.log('concatenated', concatenated.first, concatenated.second, concatenated.third);
const structural: {first: boolean; second: boolean} = fresh();
structural.first = true;
console.log('structural', structural.first, structural.second);

const proxyTarget = fresh();
let traps = 0;
const proxy = new Proxy(proxyTarget, {set(target, key, value, receiver) { traps++; return Reflect.set(target, key, value, receiver); }});
proxy.first = true;
console.log('proxy', typed(proxy), typed(proxyTarget), traps);

class Wide {
    a00 = true; a01 = false; a02 = false; a03 = false;
    a04 = false; a05 = false; a06 = false; a07 = false;
    a08 = false; a09 = false; a10 = false; a11 = false;
    a12 = false; a13 = false; a14 = false; a15 = false;
    a16 = true; a17 = false; a18 = false; a19 = false;
    a20 = false; a21 = false; a22 = false; a23 = false;
    a24 = false; a25 = false; a26 = false; a27 = false;
    a28 = false; a29 = false; a30 = false; a31 = true;
}
function wide(items: Wide[]): number {
    let sum = 0;
    for (let i = 0; i < items.length; i++)
        sum += (items[i].a00 ? 1 : 0) + (items[i].a15 ? 2 : 0) + (items[i].a16 ? 4 : 0) + (items[i].a31 ? 8 : 0);
    return sum;
}
const wideFlags = new Wide();
console.log('wide', wide([wideFlags]), Object.hasOwn(wideFlags, 'a31'));
wideFlags.a15 = true;
wideFlags.a31 = undefined as any;
console.log('wide writes', wide([wideFlags]), wideFlags.a31, Object.hasOwn(wideFlags, 'a31'));
wideFlags.a31 = false;
wideFlags.a16 = false;
console.log('wide restored', wide([wideFlags]), wideFlags.a31);
