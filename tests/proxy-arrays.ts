const numbers = new Proxy([1, 2, 3], {});
console.log(JSON.stringify(numbers));
console.log([0].concat(numbers).join(","));
console.log([numbers].flat().join(","));
console.log(JSON.stringify({ a: 1, b: 2 }, new Proxy(["b"], {})));
numbers.push(4); console.log(numbers.pop(), numbers.join(","));
numbers.length = 2; console.log(numbers.join(","));
try { structuredClone(numbers); } catch (error) { console.log((error as Error).name); }
const frozen = Object.freeze({ x: 3 });
const getWrong = new Proxy(frozen, { get() { return 4; } });
try { console.log(getWrong.x); } catch { console.log("fixed get rejected"); }
const setWrong = new Proxy(frozen, { set() { return true; } });
try { (setWrong as { x: number }).x = 4; } catch { console.log("fixed set rejected"); }
const hasWrong = new Proxy(frozen, { has() { return false; } });
try { console.log("x" in hasWrong); } catch { console.log("fixed has rejected"); }
const deleteWrong = new Proxy(frozen, { deleteProperty() { return true; } });
try { console.log(Reflect.deleteProperty(deleteWrong, "x")); } catch { console.log("fixed delete rejected"); }
const keysWrong = new Proxy(frozen, { ownKeys() { return []; } });
try { console.log(Reflect.ownKeys(keysWrong)); } catch { console.log("fixed keys rejected"); }
const doubles = new Proxy({}, { ownKeys() { return ["x", "x"]; } });
try { Reflect.ownKeys(doubles); } catch { console.log("duplicate keys rejected"); }
const badConstruct = new Proxy(function () {}, { construct() { return 3 as any; } });
try { new (badConstruct as any)(); } catch { console.log("primitive construction rejected"); }
