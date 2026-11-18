export {};
class Point { value = 3; add(offset: number): number { return this.value + offset; } }
function sum(point: Point, count: number, offset = 7): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += point.add(offset);
    return total;
}
function literal(point: Point): number {
    let total = 0;
    for (let i = 0; i < 64; i++) total += point.add(7);
    return total;
}
function forward(point: Point, offset: number): number { return point.add(offset); }
function forwarded(point: Point): number {
    let total = 0;
    const offset = 7;
    for (let i = 0; i < 64; i++) total += forward(point, offset);
    return total;
}
function dependent(point: Point): number {
    let total = 1;
    for (let i = 0; i < 64; i++) total += point.add(total);
    return total;
}
const point = new Point();
console.log(sum(point, 64), literal(point), forwarded(point), dependent(point));
console.log(sum(point, 0), sum(point, 31), sum(point, 32), sum(point, 64, NaN));
console.log(sum(undefined as unknown as Point, 0));
let reads = 0;
Object.defineProperty(point, 'value', {configurable: true, get() { reads++; return reads; }});
console.log(sum(point, 64), reads);
Object.defineProperty(point, 'value', {configurable: true, writable: true, value: 4});
let calls = 0;
Object.defineProperty(point, 'add', {configurable: true, value: (offset: number): number => { calls++; return offset + calls; }});
console.log(sum(point, 64), calls);
delete (point as any).add;
console.log(sum(point, 64));
class Derived extends Point { add(offset: number): number { return offset * 2; } }
console.log(sum(new Derived(), 64));
let changingOffset = 1;
const custom = new Point();
Object.defineProperty(custom, 'add', {value: (offset: number): number => { changingOffset++; return offset; }});
function changing(): number {
    let total = 0;
    for (let i = 0; i < 64; i++) total += custom.add(changingOffset);
    return total;
}
console.log(changing(), changingOffset);
const left = new Point(), right = new Point();
Object.defineProperty(right, 'add', {get() { console.log('method before argument'); return Point.prototype.add; }});
function conditional(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += (i & 1 ? left : right).add(lateOffset);
    return total;
}
console.log(conditional(0));
try { conditional(64); } catch (error) { console.log((error as Error).name); }
function uninitialized(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += point.add(lateOffset);
    return total;
}
console.log(uninitialized(0));
try { uninitialized(64); } catch (error) { console.log((error as Error).name); }
const lateOffset = 1;
console.log(uninitialized(64));
