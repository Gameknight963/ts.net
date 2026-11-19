export {};
function adjust(value: number): number { return value * 2 + 1; }
function nested(value: number): number { return -adjust(value / 2); }
class Point {
    x = 0; y = 0;
    constructor(x: number, y: number) { this.x = adjust(x); this.y = nested(y - x); }
}
const points: Point[] = [];
for (let i = 0; i < 8; i++) points.push(new Point(i, i + 2));
console.log(points.map(p => `${p.x}:${p.y}`).join(','));
for (const value of [-0, NaN, Infinity, -Infinity, undefined as any]) {
    const point = new Point(value, 2);
    console.log(point.x, point.y, Object.is(point.x, -0));
}
function identity(value: number): number { return value; }
class Zero { value = 1; constructor(value: number) { this.value = identity(value); } }
console.log(Object.is(new Zero(-0).value, -0));

// A helper that changes push must execute once per constructor, in order.
const originalPush = Array.prototype.push;
let calls = 0;
function change(value: number): number {
    calls++;
    Array.prototype.push = function (...items: any[]) { calls += 10; return originalPush.apply(this, items); };
    return value;
}
class Effect { value = 0; constructor(value: number) { this.value = change(value); } }
const effects: Effect[] = [];
for (let i = 0; i < 3; i++) effects.push(new Effect(i));
Array.prototype.push = originalPush;
console.log(calls, effects.map(p => p.value).join(','));
let replacement = (value: number) => value;
class Changed { value = 0; constructor(value: number) { this.value = replacement(value); } }
console.log(new Changed(2).value);
replacement = value => value + 10;
console.log(new Changed(2).value);
let captured = 5;
function capture(value: number): number { return value + captured; }
class Captured { value = 0; constructor(value: number) { this.value = capture(value); } }
console.log(new Captured(2).value); captured = 10; console.log(new Captured(2).value);
function defaulted(value = 7): number { return value; }
class Defaulted { value = 0; constructor(value: number) { this.value = defaulted(value); } }
console.log(new Defaulted(undefined as any).value);
function cycle(value: number): number { return cycle(value); }
class Cycle { value = 0; constructor(value: number) { this.value = cycle(value); } }
if (false) console.log(new Cycle(1));
