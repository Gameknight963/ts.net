export {};
const value = { base: 2, read(amount: number): number { return this.base + amount; } };
const read = value.read as (this: any, amount: number) => number;
console.log("own", value.read(3));
value.base = 7;
console.log("write", value.read(3));
console.log("borrowed", read.call({ base: 11 }, 3));
console.log("inherited", read.call(Object.create({ base: 13 }), 3));
let gets = 0;
console.log("borrowed getter", read.call({ get base() { gets++; return 17; } }, 3), gets);
Object.defineProperty(value, "base", { configurable: true, get() { gets++; return 19; } });
console.log("own getter", value.read(3), gets);
Object.defineProperty(value, "base", { configurable: true, value: 23, writable: true });
console.log("replace getter", value.read(3));
const prototype = { get base() { gets++; return 29; } };
Object.setPrototypeOf(value, prototype);
delete (value as any).base;
console.log("deleted", value.read(3), gets);

const other = { base: -0, read(amount: number): number { return this.base * amount; } };
console.log("signed zero", Object.is(other.read(2), -0));
const replacement = other.read;
other.read = read;
console.log("replaced method", other.read(2), replacement.call({ base: 5, read: replacement }, 2));

const closure = { base: 31, create() { return () => this.base + 1; } };
const next = closure.create();
console.log("closure", next());
closure.base = 37;
console.log("closure write", next());

const throwing = { base: 1, read(): number { return this.base; } };
Object.defineProperty(throwing, "base", { get() { throw "getter"; } });
try { throwing.read(); } catch (error) { console.log("throw", error); }

const updating = {
    base: 1,
    add(amount: number): number {return this.base += amount;},
    step(): number {return this.base++;}
};
const update = updating.add as (this: any, amount: number) => number;
console.log('updates', updating.add(2), updating.step(), updating.base);
console.log('borrowed update', update.call({base: 10}, 3));
let stored = 20, reads = 0, writes = 0;
const accessor = {get base() {reads++; return stored;}, set base(value: number) {writes++; stored = value;}};
console.log('accessor update', update.call(accessor, 4), stored, reads, writes);
Object.defineProperty(updating, 'base', {get() {reads++; return stored;}, set(value: number) {writes++; stored = value;}, configurable: true});
console.log('changed update', updating.add(5), stored, reads, writes);
const proxied = new Proxy({base: 30}, {get(target, key, receiver) {
    console.log('get update', String(key)); return Reflect.get(target, key, receiver);
}, set(target, key, value, receiver) {
    console.log('set update', String(key), value); return Reflect.set(target, key, value, receiver);
}});
console.log('proxy update', update.call(proxied, 6));

const assignments = {base: 0, write(value: number): void {this.base = value;}};
assignments.write(-0);
console.log('assigned zero', Object.is(assignments.base, -0));
assignments.write(undefined as unknown as number);
console.log('assigned undefined', assignments.base);
Object.freeze(assignments);
try {assignments.write(1);} catch {console.log('frozen method write', assignments.base);}
Object.defineProperty(updating, 'base', {value: 50, writable: false, configurable: true});
try {updating.add(1);} catch {console.log('readonly method update', updating.base);}
