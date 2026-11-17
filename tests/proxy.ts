const target = { value: 3 };
const proxy = new Proxy(target, {
  get(object, key, receiver) {
    if (key === "value") return Reflect.get(object, key, receiver) * 2;
    return Reflect.get(object, key, receiver);
  },
  set(object, key, value, receiver) {
    return Reflect.set(object, key, value, receiver);
  }
});
console.log(proxy.value);
proxy.value = 5;
console.log(target.value, proxy.value, "value" in proxy);
console.log(Object.keys(proxy).join(","));
const rejected = new Proxy(target, { set() { return false; } });
console.log(Reflect.set(rejected, "value", 9), target.value);
try { rejected.value = 9; } catch { console.log("assignment rejected"); }
const callable = new Proxy((x: number) => x + 1, {
  apply(fn, receiver, args) { return Reflect.apply(fn, receiver, args) * 3; }
});
console.log(callable(4));
const revocable = Proxy.revocable(target, {});
console.log(revocable.proxy.value);
revocable.revoke();
try { console.log(revocable.proxy.value); } catch { console.log("revoked"); }
