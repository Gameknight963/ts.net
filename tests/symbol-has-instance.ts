class Even { static [Symbol.hasInstance](value: any): boolean { return typeof value === "number" && value % 2 === 0; } }
console.log("class", (2 as any) instanceof Even, (3 as any) instanceof Even, ({} as any) instanceof Even);
class Derived extends Even {}
console.log("inherited", (4 as any) instanceof Derived);
const matcher = {[Symbol.hasInstance](value: any) { console.log("receiver", this === matcher, arguments.length); return value === "yes"; }};
console.log("object", ("yes" as any) instanceof matcher, ("no" as any) instanceof matcher);
for (const returned of [0, 1, "", "text", null, undefined, {}, false, true]) {
    const target = {[Symbol.hasInstance]() { return returned; }};
    console.log("truthy", (null as any) instanceof (target as any));
}
let reads = 0;
const getter: any = {get [Symbol.hasInstance]() { reads++; return (value: any) => value === 7; }};
console.log("getter", (7 as any) instanceof getter, reads);
class Plain {}
const plain = new Plain();
console.log("ordinary", plain instanceof Plain, ({} as any) instanceof Plain);
const bound = Even.bind(null);
console.log("bound", (8 as any) instanceof bound, (9 as any) instanceof bound);
for (const target of [null, undefined, 1, {}, {[Symbol.hasInstance]: 1}, {[Symbol.hasInstance]() {throw "matcher failure"; }}]) {
    try { console.log(({} as any) instanceof (target as any)); } catch (error) { console.log("error", typeof error === "string" ? error : (error as Error).name); }
}
const ordinary = (Object.getPrototypeOf(function() {}) as any)[Symbol.hasInstance];
console.log("ordinary method", ordinary.call(Plain, plain), ordinary.call(Plain, {}), ordinary.call({}, plain), ordinary.name, ordinary.length);
class Combined { static [Symbol.hasInstance](value: any): boolean { return ordinary.call(this, value) || value === "special"; } }
console.log("combined", new Combined() instanceof Combined, ("special" as any) instanceof Combined, ({} as any) instanceof Combined);
const descriptor = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(Plain), Symbol.hasInstance)!;
console.log("descriptor", descriptor.writable, descriptor.enumerable, descriptor.configurable);
