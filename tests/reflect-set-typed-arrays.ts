for (const key of ["0", "-0", "-1", "NaN", "Infinity", "01"]) {
 const receiver = new Uint8Array(0);
 console.log("receiver", key, Reflect.set({}, key, 7, receiver), Object.hasOwn(receiver, key));
 const target = new Uint8Array(0);
 console.log("target", key, Reflect.set(target, key, 7), Object.hasOwn(target, key));
}
let conversions = 0;
const value = { valueOf() { conversions++; return 9; } };
console.log("invalid receiver", Reflect.set({}, "0", value, new Uint8Array(0)), conversions);
console.log("invalid target", Reflect.set(new Uint8Array(0), "0", value), conversions);
const valid = new Uint8Array(1);
console.log("valid receiver", Reflect.set({}, "0", value, valid), valid[0], conversions);
const other: Record<string, unknown> = {};
console.log("other receiver", Reflect.set(new Uint8Array(0), "0", value, other), other["0"] === value, conversions);
const validTarget = new Uint8Array([2]);
const plainReceiver: Record<string, unknown> = {};
console.log("valid other", Reflect.set(validTarget, "0", value, plainReceiver), plainReceiver["0"] === value, conversions, validTarget[0]);
console.log("invalid primitive", Reflect.set(new Uint8Array(0), "0", 4, 1));
console.log("valid primitive", Reflect.set(validTarget, "0", 4, 1));
const bigintReceiver = new BigInt64Array(0);
console.log("bigint receiver", Reflect.set({}, "0", value, bigintReceiver), conversions);
console.log("bigint other", Reflect.set(bigintReceiver, "0", 3, {}));
try { Reflect.set(bigintReceiver, "0", 3); } catch { console.log("bigint conversion rejected"); }
let definitions = 0;
const wrapped = new Proxy(new Uint8Array(0), { defineProperty() { definitions++; return true; } });
console.log("claimed definition", Reflect.set({}, "0", value, wrapped), definitions, conversions);
