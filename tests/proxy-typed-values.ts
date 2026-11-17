const seen: string[] = [];
const array = new Proxy([2, 4, 6], {
 get(object, key, receiver) { seen.push(String(key)); return Reflect.get(object, key, receiver); },
 set(object, key, value, receiver) { seen.push("set:" + String(key)); return Reflect.set(object, key, value, receiver); }
});
console.log(array.length, array[1]);
array[1] = 9;
console.log(array.join(","));
console.log(array.map(x => x * 2).join(","));
console.log([...array].join(","));
console.log(seen.join("|"));
class Box { constructor(public value: number) {} read() { return this.value; } }
const box = new Proxy(new Box(3), { get(object, key, receiver) { return key === "value" ? 11 : Reflect.get(object, key, receiver); } });
console.log(box.value, box.read(), box instanceof Box);
const Wrapped = new Proxy(Box, {
 construct(ctor, args, newTarget) { return Reflect.construct(ctor, [args[0] * 2], newTarget); }
});
const made = new Wrapped(5);
console.log(made.value, made instanceof Box, made instanceof Wrapped);
const object = { value: 4, method(amount: number) { return this.value + amount; } };
object.method = new Proxy(object.method, { apply(fn, receiver, args) { return Reflect.apply(fn, receiver, args) * 2; } });
console.log(object.method(3));
const key = Symbol("key");
const symbolTarget = { [key]: 8 };
const symbolProxy = new Proxy(symbolTarget, { get(obj, property, receiver) { console.log(property === key); return Reflect.get(obj, property, receiver); } });
console.log(symbolProxy[key]);
