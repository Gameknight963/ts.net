export {};
class Counter {
    total = 0;
    step(): void {this.total++;}
    down(): void {--this.total;}
    add(): void {this.total += 2;}
}
function invoke(target: Counter): void {target.step();}
function forwarded(target: Counter, count: number): void {for (let i = 0; i < count; i++) invoke(target);}
function direct(target: Counter, count: number): void {for (let i = 0; i < count; i++) target.step();}
function decrement(target: Counter): void {for (let i = 0; i < 40; i++) target.down();}
function add(target: Counter): void {for (let i = 0; i < 40; i++) target.add();}
const basic = new Counter();
forwarded(basic, 40); direct(basic, 40); decrement(basic); add(basic);
console.log('basic', basic.total);
const fractional = new Counter(); forwarded(fractional, 40.5); console.log('fractional', fractional.total);
forwarded(undefined as any, 0); direct(undefined as any, -1); direct(undefined as any, NaN);
console.log('empty');
const original = Counter.prototype.step;
const own = new Counter(); own.step = function(){this.total += 3;};
forwarded(own, 40); console.log('own replacement', own.total);
Counter.prototype.step = function(){this.total += 4;};
const replaced = new Counter(); direct(replaced, 40); console.log('prototype replacement', replaced.total);
Counter.prototype.step = original;
const getter = new Counter(); let reads = 0;
Object.defineProperty(getter, 'step', {get(){reads++; return original;}, configurable: true});
forwarded(getter, 40); console.log('method getter', getter.total, reads);
const changing = new Counter(); let prototypeReads = 0;
Object.defineProperty(Counter.prototype, 'step', {get(){prototypeReads++; Object.defineProperty(Counter.prototype, 'step', {value: function(){this.total += 5;}, writable: true, configurable: true}); return original;}, configurable: true});
direct(changing, 40); console.log('changing getter', changing.total, prototypeReads);
Object.defineProperty(Counter.prototype, 'step', {value: original, writable: true, configurable: true});
const accessed = new Counter(); let stored = 0, gets = 0, sets = 0;
Object.defineProperty(accessed, 'total', {get(){gets++; return stored;}, set(value){sets++; stored = value;}, configurable: true});
forwarded(accessed, 40); console.log('field getter', stored, gets, sets);
const proxied = new Counter(); let traps = 0;
const proxy = new Proxy(proxied, {get(target, key, receiver){traps++; return Reflect.get(target, key, receiver);}});
direct(proxy, 40); console.log('proxy', proxied.total, traps);
const missing = new Counter(); delete (missing as any).total;
Object.defineProperty(Counter.prototype, 'total', {value: 10, writable: true, configurable: true});
forwarded(missing, 40); console.log('inherited field', missing.total, Object.hasOwn(missing, 'total'));
delete (Counter.prototype as any).total;
const undefinedField = new Counter(); undefinedField.total = undefined as any;
forwarded(undefinedField, 40); console.log('undefined field', Number.isNaN(undefinedField.total));
const frozen = Object.freeze(new Counter()); let threw = false;
try {direct(frozen, 40);} catch {threw = true;}
console.log('frozen', threw, frozen.total);
class Derived extends Counter {step(): void {this.total += 6;}}
const derived = new Derived(); forwarded(derived, 40); console.log('derived', derived.total);
const structural: Counter = {total: 0, step(){this.total += 7;}, down(){}, add(){}};
forwarded(structural, 40); console.log('structural', structural.total);
const customPrototype = new Counter();
Object.setPrototypeOf(customPrototype, {step(this: Counter){this.total += 8;}});
direct(customPrototype, 40); console.log('custom prototype', customPrototype.total);
let calls = 0;
class ImpureCounter {total = 0; step(): void {this.total++; calls++;}}
function impure(target: ImpureCounter): void {for (let i = 0; i < 40; i++) target.step();}
const impureCounter = new ImpureCounter(); impure(impureCounter);
console.log('side effects', impureCounter.total, calls);
const rounded = new Counter(); rounded.total = 9007199254740992;
forwarded(rounded, 40); console.log('rounded increments', rounded.total);
class FractionalCounter {total = 0.1; step(): void {this.total += 0.2;}}
function fractionalUpdates(target: FractionalCounter): void {for (let i = 0; i < 40; i++) target.step();}
const fractions = new FractionalCounter(); fractionalUpdates(fractions);
console.log('fractional updates', fractions.total);
class Subtractor {total = -0; step(): void {this.total -= 0;}}
function subtractZero(target: Subtractor): void {for (let i = 0; i < 40; i++) target.step();}
const zero = new Subtractor(); subtractZero(zero); console.log('negative zero', Object.is(zero.total, -0));
const notFinite = new Counter(); notFinite.total = Infinity; direct(notFinite, 40);
console.log('infinity', notFinite.total);
notFinite.total = NaN; direct(notFinite, 40); console.log('nan', Number.isNaN(notFinite.total));
class WideCounter {
    a0=0; a1=1; a2=2; a3=3; a4=4; a5=5; a6=6; a7=7;
    a8=8; a9=9; a10=10; a11=11; a12=12; a13=13; a14=14; a15=15;
    a16=16; a17=17; a18=18; a19=19; a20=20; a21=21; a22=22; a23=23;
    a24=24; a25=25; a26=26; a27=27; a28=28; a29=29; a30=30; total=31;
    step(): void {this.total++;}
}
function wideUpdates(target: WideCounter): void {for (let i = 5; i < 50; i++) target.step();}
const wide = new WideCounter(); wideUpdates(wide); console.log('wide field', wide.total, wide.a30);
const described = new Counter(); Object.defineProperty(described, 'total', {value: 12, writable: true});
forwarded(described, 40); console.log('described data', described.total);
const readonly = new Counter(); Object.defineProperty(readonly, 'total', {value: 12, writable: false});
let readonlyThrew = false; try {forwarded(readonly, 40);} catch {readonlyThrew = true;}
console.log('readonly data', readonlyThrew, readonly.total);
