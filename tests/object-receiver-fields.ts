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
