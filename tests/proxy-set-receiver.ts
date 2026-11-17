const source = { value: 1 };
const calls: string[] = [];
const receiverTarget: Record<string, unknown> = {};
const receiver = new Proxy(receiverTarget, {
 getOwnPropertyDescriptor(target, key) { calls.push("describe:" + String(key)); return Reflect.getOwnPropertyDescriptor(target, key); },
 isExtensible() { calls.push("unexpected isExtensible"); throw new Error("wrong trap"); },
 defineProperty(target, key, descriptor) { calls.push("define:" + String(key)); return Reflect.defineProperty(target, key, descriptor); }
});
try { console.log(Reflect.set(source, "value", 3, receiver)); } catch (error) { console.log((error as Error).message); }
console.log(receiverTarget.value, calls.join("|"));
const sealedTarget = Object.preventExtensions({});
const rejected = new Proxy(sealedTarget, { defineProperty() { console.log("rejected define"); return false; } });
console.log(Reflect.set(source, "value", 4, rejected));
const lying = new Proxy(sealedTarget, { defineProperty() { console.log("lying define"); return true; } });
try { console.log(Reflect.set(source, "value", 5, lying)); } catch { console.log("definition invariant rejected"); }
const existing = new Proxy({ value: 2 }, { isExtensible() { throw new Error("wrong existing trap"); } });
console.log(Reflect.set(source, "value", 7, existing), existing.value);
const forwardedTarget: Record<string, number> = {};
const forwarded = new Proxy(forwardedTarget, { isExtensible() { throw new Error("wrong assignment trap"); } });
forwarded.value = 8;
console.log(forwardedTarget.value);
const nonExtensible = new Proxy(Object.preventExtensions({}), { isExtensible() { throw new Error("wrong rejection trap"); } });
console.log(Reflect.set(nonExtensible, "value", 9));
