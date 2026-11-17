function check(mode: "freeze" | "seal") {
 const target = { value: 1 };
 const calls: string[] = [];
 const proxy = new Proxy(target, {
  isExtensible(object) { calls.push("isExtensible"); return Reflect.isExtensible(object); },
  preventExtensions(object) { calls.push("preventExtensions"); return Reflect.preventExtensions(object); },
  ownKeys(object) { calls.push("ownKeys"); return Reflect.ownKeys(object); },
  getOwnPropertyDescriptor(object, key) { calls.push("describe:" + String(key)); return Reflect.getOwnPropertyDescriptor(object, key); },
  defineProperty(object, key, descriptor) { calls.push("define:" + String(key)); return Reflect.defineProperty(object, key, descriptor); }
 });
 if (mode === "freeze") Object.freeze(proxy); else Object.seal(proxy);
 console.log(mode, calls.join("|"));
 console.log(Object.isExtensible(target), Object.isSealed(target), Object.isFrozen(target));
 calls.length = 0;
 if (mode === "freeze") Object.freeze(proxy); else Object.seal(proxy);
 console.log("repeat", calls.join("|"));
}
check("freeze"); check("seal");
const alreadyFrozen = Object.freeze({ value: 1 });
const rejected = new Proxy(alreadyFrozen, { preventExtensions() { console.log("rejected trap"); return false; } });
try { Object.freeze(rejected); } catch { console.log("freeze rejected"); }
try { Object.seal(rejected); } catch { console.log("seal rejected"); }
const pair = { a: 1, b: 2 };
const deleting = new Proxy(pair, { defineProperty(object, key, descriptor) {
 console.log("define", String(key));
 if (key === "a") delete (object as { b?: number }).b;
 return Reflect.defineProperty(object, key, descriptor);
} });
try { Object.seal(deleting); } catch { console.log("removed key rejected"); }
let getterCalls = 0;
const accessor = { get value() { getterCalls++; return 4; } };
Object.freeze(new Proxy(accessor, {}));
console.log("getter calls", getterCalls, Object.isFrozen(accessor));
const array = [1, 2];
const arrayProxy = new Proxy(array, {});
Object.freeze(arrayProxy);
console.log("array frozen", Object.isFrozen(array), Object.isFrozen(arrayProxy));
try { arrayProxy.push(3); } catch { console.log("frozen push rejected"); }
const interrupted = new Proxy({}, { preventExtensions() { throw new Error("blocked"); }, ownKeys() { throw new Error("wrong trap"); } });
try { Object.freeze(interrupted); } catch (error) { console.log((error as Error).message); }
