const base = { get value() { return (this as any).marker; }, set value(n: number) { (this as any).marker = n + 1; } };
const proxy = new Proxy(base, {});
const receiver = { marker: 5 };
console.log(Reflect.get(proxy, "value", receiver));
console.log(Reflect.set(proxy, "value", 8, receiver), receiver.marker);
const child = Object.create(proxy);
child.marker = 12;
console.log(child.value);
// A trap getter can revoke the proxy whose operation is already in progress.
let revoke: () => void = () => {};
const current = Proxy.revocable({ x: 4 }, { get get() { revoke(); return (object: {x:number}) => object.x * 2; } });
revoke = current.revoke;
console.log(current.proxy.x);
try { console.log(current.proxy.x); } catch { console.log("next operation revoked"); }
const arrow = Proxy.revocable((n: number) => n + 1, {});
console.log(arrow.proxy(2), typeof arrow.proxy);
arrow.revoke(); console.log(typeof arrow.proxy);
try { arrow.proxy(2); } catch { console.log("revoked call rejected"); }
class Base { constructor(public value: number) {} }
const Constructor = new Proxy(Base, {});
class Derived extends Constructor { double() { return this.value * 2; } }
const result = new Derived(6);
console.log(result.double(), result instanceof Derived, result instanceof Base);
const descriptorTarget = Object.create(null);
Object.defineProperty(descriptorTarget, "fixed", { value: 1, configurable: false, writable: true });
const badDescriptor = new Proxy(descriptorTarget, { getOwnPropertyDescriptor() { return { value: 1, writable: false, configurable: false }; } });
try { Object.getOwnPropertyDescriptor(badDescriptor, "fixed"); } catch { console.log("readonly lie rejected"); }
const badDefinition = new Proxy(descriptorTarget, { defineProperty() { return true; } });
try { Reflect.defineProperty(badDefinition, "fixed", { writable: false }); } catch { console.log("readonly definition lie rejected"); }
const nested = new Proxy(new Proxy({ value: 7 }, {}), {});
console.log(nested.value);
