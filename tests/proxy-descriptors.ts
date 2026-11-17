const target: any = Object.create(null);
target.x = 4;
const events: string[] = [];
const proxy = new Proxy(target, {
 getOwnPropertyDescriptor(object, key) { events.push("describe:" + String(key)); return Reflect.getOwnPropertyDescriptor(object, key); },
 defineProperty(object, key, descriptor) { events.push("define:" + String(key) + ":" + Object.keys(descriptor).sort().join(",")); return Reflect.defineProperty(object, key, descriptor); }
});
console.log(Object.getOwnPropertyDescriptor(proxy, "x")!.value);
proxy.x = 7;
console.log(target.x);
Object.defineProperty(proxy, "hidden", { value: 9 });
console.log(proxy.hidden, Object.keys(proxy).join(","));
console.log(events.join("|"));
const denied = new Proxy(target, { defineProperty() { return false; } });
console.log(Reflect.set(denied, "x", 12), target.x);
console.log(Reflect.defineProperty(denied, "y", { value: 1 }));
try { Object.defineProperty(denied, "y", { value: 1 }); } catch { console.log("definition rejected"); }
const hidden = new Proxy(target, { getOwnPropertyDescriptor() { return undefined; } });
console.log(Object.hasOwn(hidden, "x"));
try { Object.hasOwn(hidden, "hidden"); } catch { console.log("fixed descriptor cannot disappear"); }
const invented = new Proxy(target, { getOwnPropertyDescriptor() { return { value: 1 }; } });
try { Object.getOwnPropertyDescriptor(invented, "absent"); } catch { console.log("fixed descriptor cannot appear"); }
const fakeFixed = new Proxy(target, { defineProperty() { return true; } });
try { Reflect.defineProperty(fakeFixed, "absent", { value: 1, configurable: false }); } catch { console.log("fixed definition must exist"); }
const key = Symbol("key");
target[key] = 10;
const symbols = new Proxy(target, { ownKeys(object) { return Reflect.ownKeys(object).reverse(); } });
console.log(Reflect.ownKeys(symbols).map(String).join(","));
console.log(Array.isArray(new Proxy([1, 2], {})));
const revoked = Proxy.revocable([], {}); revoked.revoke();
try { Array.isArray(revoked.proxy); } catch { console.log("revoked array rejected"); }
