const prototypeObject = { marker: 7 };
const target = Object.create(prototypeObject);
const calls: string[] = [];
const proxy = new Proxy(target, {
 getPrototypeOf(object) { calls.push("getPrototypeOf"); return Reflect.getPrototypeOf(object); },
 setPrototypeOf(object, prototype) { calls.push("setPrototypeOf"); return Reflect.setPrototypeOf(object, prototype); },
 isExtensible(object) { calls.push("isExtensible"); return Reflect.isExtensible(object); },
 preventExtensions(object) { calls.push("preventExtensions"); return Reflect.preventExtensions(object); }
});
console.log(Object.getPrototypeOf(proxy) === prototypeObject);
console.log(Reflect.setPrototypeOf(proxy, null));
console.log(Reflect.isExtensible(proxy));
console.log(Reflect.preventExtensions(proxy));
console.log(Reflect.isExtensible(proxy));
console.log(calls.join(","));
const lying = new Proxy({}, { preventExtensions() { return true; } });
try { Reflect.preventExtensions(lying); } catch { console.log("invalid success rejected"); }
const refused = new Proxy({}, { preventExtensions() { return false; } });
console.log(Reflect.preventExtensions(refused));
try { Object.preventExtensions(refused); } catch { console.log("object failure throws"); }
const fixed = Object.preventExtensions({});
const wrong = new Proxy(fixed, { getPrototypeOf() { return null; }, isExtensible() { return true; } });
try { Object.getPrototypeOf(wrong); } catch { console.log("wrong prototype rejected"); }
try { Reflect.isExtensible(wrong); } catch { console.log("wrong extensibility rejected"); }
