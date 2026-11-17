export {};
class Point {
    x = 0; y = 0; ratio = 0; negative = 0;
    constructor(x: number, y: number) {
        this.x = x * 2 + 1;
        this.y = (y - x) / 2;
        this.ratio = x / y;
        this.negative = -x;
    }
}
function local(x: number, y: number): number {
    const point = new Point(x, y);
    return point.x + point.y + point.ratio + point.negative;
}
function kernel(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const point = new Point(i, i + 2);
        total += point.x + point.y;
    }
    return total;
}
function escape(): any { return new Point(3, 5); }
console.log('kernel', kernel(10), 'escape', escape().x);
const point = new Point(4, 8);
console.log(point.x, point.y, point.ratio, point.negative, local(4, 8));
console.log(Object.keys(point).join(','), point instanceof Point, Object.getPrototypeOf(point) === Point.prototype);
const missing = new Point(undefined as unknown as number, 2);
console.log(Number.isNaN(missing.x), Number.isNaN(missing.y), Number.isNaN(missing.ratio), Number.isNaN(missing.negative));
console.log(Object.hasOwn(missing, 'x'), Object.keys(missing).join(','));
const zero = new Point(0, 0), infinity = new Point(1, 0);
console.log(Object.is(zero.negative, -0), Number.isNaN(zero.ratio), infinity.ratio);
let trace = '';
function argument(label: string, value: number): number { trace += label; return value; }
const ordered = new Point(argument('x', 3), argument('y', 7));
console.log(trace, ordered.x, ordered.y);
class Repeated {
    value = 0;
    constructor(value: number) { this.value = value + 1; this.value = value * 3; }
}
console.log(new Repeated(5).value);
class ParameterField { constructor(public value: number) {} }
console.log(new ParameterField(6).value);
let effects = 0;
class Effect {
    value = 0;
    constructor(value: number) { this.value = argument('effect', value) + ++effects; }
}
console.log(new Effect(9).value, effects, trace);
class ReadOwn {
    value = 2; result = 0;
    constructor(value: number) { this.result = this.value + value; }
}
console.log(new ReadOwn(10).result);
class Default {
    value = 0;
    constructor(value = 4) { this.value = value * 2; }
}
console.log(new Default().value, new Default(undefined).value, new Default(3).value);
const changed = new Point(2, 4);
Object.defineProperty(changed, 'x', { get() { return 99; }, configurable: true });
console.log(changed.x, changed.y);
let received = 0;
class InheritedSetter {
    declare value: number;
    constructor(value: number) { this.value = value * 2; }
}
Object.defineProperty(InheritedSetter.prototype, 'value', { set(value: number) { received = value; }, configurable: true });
const inherited = new InheritedSetter(7);
console.log(received, Object.hasOwn(inherited, 'value'));
Object.defineProperty(Point.prototype, 'x', { set() { throw new Error('own field must win'); }, configurable: true });
console.log(new Point(5, 7).x, local(5, 7));
delete (Point.prototype as any).x;
