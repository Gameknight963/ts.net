export {};
class Wide {
    f0 = 1; f1 = 2; f2 = 3; f3 = 4;
    f4 = 5; f5 = 6; f6 = 7; f7 = 8;
    f8 = 9; f9 = 10; f10 = 11; f11 = 12;
    f12 = 13; f13 = 14; f14 = 15; f15 = 16;
    f16 = 17; f17 = 18; f18 = 19; f19 = 20;
    f20 = 21; f21 = 22; f22 = 23; f23 = 24;
    f24 = 25; f25 = 26; f26 = 27; f27 = 28;
    f28 = 29; f29 = 30; f30 = 31; f31 = 32;
}
function sum(values: Wide[]): number {
    let result = 0;
    for (let i = 0; i < values.length; i++) result += values[i].f0 + values[i].f16 + values[i].f31;
    return result;
}
const first = new Wide();
console.log('wide', first.f0, first.f15, first.f16, first.f30, first.f31, sum([first, new Wide()]));
first.f16 += 2; first.f31++;
console.log('updates', first.f0, first.f15, first.f16, first.f30, first.f31, sum([first]));
(first as any).f31 = undefined;
console.log('undefined high', first.f0, first.f15, first.f16, first.f31, Object.hasOwn(first, 'f31'), Number.isNaN(sum([first])));
first.f31 = 40;
console.log('restore high', sum([first]));
console.log('keys', Object.keys(first).join(','));
console.log('after materialization', first.f16, first.f31, sum([first]));
delete (first as any).f31;
let gets = 0;
Object.defineProperty(Wide.prototype, 'f31', {get() {gets++; return 90;}, configurable: true});
console.log('deleted high', sum([first]), gets, Object.hasOwn(first, 'f31'));
Object.defineProperty(first, 'f16', {get() {gets++; return 50;}, configurable: true});
console.log('getter high', sum([first]), gets);
delete (Wide.prototype as any).f31;
const frozen = new Wide(); Object.freeze(frozen);
try {frozen.f31 = 100;} catch (error) {console.log('frozen', (error as Error).name);}
console.log('frozen fields', frozen.f31, sum([frozen]));
class Parameters {
    f0 = 1; f1 = 2; f2 = 3; f3 = 4;
    f4 = 5; f5 = 6; f6 = 7; f7 = 8;
    f8 = 9; f9 = 10; f10 = 11; f11 = 12;
    f12 = 13; f13 = 14; f14 = 15; f15 = 16;
    f16 = 17; f17 = 18; f18 = 19; f19 = 20;
    f20 = 21; f21 = 22; f22 = 23; f23 = 24;
    f24 = 25; f25 = 26; f26 = 27; f27 = 28;
    f28 = 29; f29 = 30; f30 = 31; f31 = 32;
    constructor(value: number) {this.f0 = value; this.f16 = value + 1; this.f31 = value + 2;}
}
const parameterized = new Parameters(undefined as unknown as number);
console.log('parameter states', parameterized.f0, parameterized.f16, parameterized.f31, Object.keys(parameterized).length);
const ordinary = new Parameters(10);
console.log('parameter values', ordinary.f0, ordinary.f15, ordinary.f16, ordinary.f31);
class Mixed {
    f0 = 1; f1 = 2; f2 = 3; f3 = 4;
    f4 = 5; f5 = 6; f6 = 7; f7 = 8;
    f8 = 9; f9 = 10; f10 = 11; f11 = 12;
    f12 = 13; f13 = 14; f14 = 15; f15 = 16;
    f16 = 'text'; f17 = 18; f18 = 19; f19 = 20;
    f20 = 21; f21 = 22; f22 = 23; f23 = 24;
    f24 = 25; f25 = 26; f26 = 27; f27 = 28;
    f28 = 29; f29 = 30; f30 = 31; payload: unknown = undefined;
}
const mixed = new Mixed();
console.log('reference fields', mixed.f0, mixed.f16, mixed.payload, Object.hasOwn(mixed, 'payload'));
mixed.f16 = 'updated'; mixed.payload = null;
console.log('reference writes', mixed.f0, mixed.f16, mixed.payload, Object.keys(mixed).length);
export function make(payload: unknown): unknown {const value = new Mixed(); value.payload = payload; return value;}
export function updates(count: number): number {
    const value: {payload: number} = new Mixed() as any;
    for (let i = 0; i < count; i++) value.payload = i;
    return value.payload;
}
let escaped: any;
function early(value: any): number {escaped = value; value.f31 = 90; return 1;}
class Ordered {
    f0 = early(this); f1 = 2; f2 = 3; f3 = 4;
    f4 = 5; f5 = 6; f6 = 7; f7 = 8;
    f8 = 9; f9 = 10; f10 = 11; f11 = 12;
    f12 = 13; f13 = 14; f14 = 15; f15 = 16;
    f16 = 17; f17 = 18; f18 = 19; f19 = 20;
    f20 = 21; f21 = 22; f22 = 23; f23 = 24;
    f24 = 25; f25 = 26; f26 = 27; f27 = 28;
    f28 = 29; f29 = 30; f30 = 31; f31 = 32;
}
const ordered = new Ordered();
console.log('early creation', escaped === ordered, ordered.f0, ordered.f16, ordered.f31, Object.keys(ordered).join(','));
class Larger {
    f0 = 1; f1 = 2; f2 = 3; f3 = 4;
    f4 = 5; f5 = 6; f6 = 7; f7 = 8;
    f8 = 9; f9 = 10; f10 = 11; f11 = 12;
    f12 = 13; f13 = 14; f14 = 15; f15 = 16;
    f16 = 17; f17 = 18; f18 = 19; f19 = 20;
    f20 = 21; f21 = 22; f22 = 23; f23 = 24;
    f24 = 25; f25 = 26; f26 = 27; f27 = 28;
    f28 = 29; f29 = 30; f30 = 31; f31 = 32;
    f32 = 33;
}
const larger = new Larger();
console.log('larger fallback', larger.f0, larger.f16, larger.f31, larger.f32, Object.keys(larger).length);
