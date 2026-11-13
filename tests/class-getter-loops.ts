class GetterLoop {
    base = 2;
    other = 3;
    get value(): number { return this.base * 2 + this.other; }
}
function sumGetter(target: GetterLoop, count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += target.value;
    return total;
}
const target = new GetterLoop();
console.log('plain', sumGetter(target, 64));
target.base = 5;
console.log('updated', sumGetter(target, 64));
let fieldReads = 0;
Object.defineProperty(target, 'base', {configurable: true, get() { fieldReads++; return fieldReads; }});
console.log('field-getter', sumGetter(target, 64), fieldReads);
Object.defineProperty(target, 'base', {configurable: true, value: 8, writable: true});
console.log('restored', sumGetter(target, 64));
let getterReads = 0;
Object.defineProperty(target, 'value', {configurable: true, get() { getterReads++; return getterReads; }});
console.log('own-getter', sumGetter(target, 64), getterReads);
delete (target as any).value;
console.log('own-removed', sumGetter(target, 64));
const descriptor = Object.getOwnPropertyDescriptor(GetterLoop.prototype, 'value')!;
try {
    Object.defineProperty(GetterLoop.prototype, 'value', {configurable: true, get() { getterReads++; return getterReads; }});
    console.log('prototype-getter', sumGetter(target, 64), getterReads);
} finally { Object.defineProperty(GetterLoop.prototype, 'value', descriptor); }
const otherPrototype = {get value() { getterReads++; return getterReads; }};
Object.setPrototypeOf(target, otherPrototype);
console.log('changed-prototype', sumGetter(target, 64), getterReads);
Object.setPrototypeOf(target, GetterLoop.prototype);
console.log('frozen', sumGetter(Object.freeze(target), 64));
console.log('empty', sumGetter(undefined as unknown as GetterLoop, 0), sumGetter(undefined as unknown as GetterLoop, NaN));
function sumUninitialized(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++)
        total += later.value;
    return total;
}
console.log('tdz-empty', sumUninitialized(0));
try { sumUninitialized(64); } catch (error) { console.log('tdz-positive', (error as Error).name); }
const later = new GetterLoop();
console.log('tdz-initialized', sumUninitialized(64));
class MissingGetter { base!: number; get value(): number { return this.base; } }
function sumMissing(target: MissingGetter, count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += target.value;
    return total;
}
console.log('undefined', Number.isNaN(sumMissing(new MissingGetter(), 64)));
class LiteralGetter { get value(): number { return -0; } }
function sumLiteral(target: LiteralGetter): number {
    let total = -0;
    for (let index = 0; index < 64; index++) total += target.value;
    return total;
}
console.log('negative-zero', Object.is(sumLiteral(new LiteralGetter()), -0));
class Replacement extends GetterLoop { get value(): number { getterReads++; return getterReads; } }
console.log('derived', sumGetter(new Replacement(), 64), getterReads);
console.log('structural', sumGetter({base: 3, other: 2, get value() { getterReads++; return getterReads; }}, 64), getterReads);
function changed(target: GetterLoop): number {
    let total = 0;
    for (let index = 0; index < 3; index++) { target.other++; total += target.value; }
    return total;
}
console.log('writes', changed(new GetterLoop()));
const cells = new GetterLoop();
cells.base = 1; cells.base = 2; cells.base = 3;
console.log('number-cell', sumGetter(cells, 32.5));
console.log('negative-empty', sumGetter(undefined as unknown as GetterLoop, -2));
const inheritedField = new GetterLoop();
delete (inheritedField as any).base;
const baseDescriptor = Object.getOwnPropertyDescriptor(GetterLoop.prototype, 'base');
try {
    Object.defineProperty(GetterLoop.prototype, 'base', {configurable: true, get() { fieldReads++; return fieldReads; }});
    console.log('inherited-field', sumGetter(inheritedField, 64), fieldReads);
} finally {
    if (baseDescriptor) Object.defineProperty(GetterLoop.prototype, 'base', baseDescriptor);
    else delete (GetterLoop.prototype as any).base;
}
