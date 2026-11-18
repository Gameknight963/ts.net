export {};

function create(value: number) {
    return {value, step(amount: number): number {this.value += amount; return this.value;}};
}
const a = create(10), b = create(20);
console.log('direct', a.step(1), b.step(2));
console.log('identity', a.step === a.step, a.step === b.step);
const method = a.step;
console.log('metadata', method.name, method.length, method.toString().includes('this.value'));
console.log('borrowed', method.call(b, 3));
const c = create(30);
const copy = {...c};
console.log('copy', copy.step === c.step, copy.step === a.step, copy.step(4), c.value);
const d = create(40);
const assigned = Object.assign({}, d);
console.log('assign', assigned.step === d.step, assigned.step(5), d.value);
const e = create(50);
const descriptor = Object.getOwnPropertyDescriptor(e, 'step')!;
console.log('descriptor', descriptor.value === e.step, descriptor.writable, descriptor.enumerable, descriptor.configurable);
const f = create(60);
const values = Object.values(f);
console.log('values', values[1] === f.step);
const g = create(70);
Object.freeze(g);
try {g.step(1);} catch {console.log('frozen', g.value);}

const h = create(80);
function replace(): number {h.step = function changed(amount: number): number {return amount * 100;}; return 2;}
console.log('captured before arguments', h.step(replace()), h.value, h.step(3));
const i = create(90);
function observe(): number {console.log('observe during argument', i.step === i.step); return 3;}
console.log('observed argument', i.step(observe()), i.value);
const j = create(100);
function remove(): number {delete (j as any).step; return 4;}
console.log('deleted argument', j.step(remove()), j.value, 'step' in j);

const k = create(110);
let reads = 0;
const replacement = function (this: typeof k, amount: number): number {return this.value - amount;};
Object.defineProperty(k, 'step', {get() {reads++; return replacement;}, configurable: true});
console.log('getter', k.step(5), reads);
const l = create(120);
const proxy = new Proxy(l, {get(target, key, receiver) {
    console.log('get', String(key)); return Reflect.get(target, key, receiver);
}, set(target, key, value, receiver) {
    console.log('set', String(key), value); return Reflect.set(target, key, value, receiver);
}});
console.log('proxy', proxy.step(6));

function closure(offset: number) {return {value: 1, read(amount: number): number {return this.value + offset + amount;}};}
console.log('closure', closure(2).read(3), closure(4).read(5));
const home = {__proto__: {read() {return 5;}}, read(): number {return super.read() + 1;}};
console.log('home', home.read());

const prototype = create(200);
const inherited = Object.create(prototype) as ReturnType<typeof create>;
inherited.value = 300;
console.log('inherited', inherited.step(7), inherited.value, prototype.value, inherited.step === prototype.step);
const m = create(400);
const entries = Object.entries(m);
console.log('entries', entries[1][1] === m.step);
const {step: extracted, ...rest} = create(500);
console.log('rest', rest.value, extracted.call({value: 600}, 8));
const n = create(700);
console.log('optional', n.step?.(9), n.step.apply(n, [10]));
Object.defineProperty(n.step, 'name', {value: 'renamed'});
console.log('metadata write', n.step.name, create(800).step.name);

function factory() {
    return {build: function (this: {value: number}, value: number): void {this.value = value;}};
}
const firstFactory = factory(), secondFactory = factory();
const Build = firstFactory.build as unknown as {new(value: number): {value: number}; prototype: object};
console.log('constructor', new Build(42).value, firstFactory.build.prototype !== secondFactory.build.prototype);
