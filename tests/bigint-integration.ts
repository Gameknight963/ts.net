const values = [3n, 1n, 2n];
console.log("array", values.map(value => value * 2n).join(","), values.filter(value => value > 1n).join(","));
console.log("reduce", values.reduce((sum, value) => sum + value, 0n));
console.log("sort", values.slice().sort((first, second) => Number(first - second)).join(","));
console.log("spread", [...new Set([1n, 2n, BigInt("1")])].join(","));
const mixed: any[] = [1, 2];
mixed[0] = 1234567890123456789n;
console.log("mixed", mixed[0], typeof mixed[0], mixed[1]);
console.log("mixed update", mixed[0]++, mixed[0]);
function identity<T>(value: T): T {return value;}
console.log("generic", identity(1234567890123456789n));
class Box<T> {constructor(public value: T) {} read(): T {return this.value;} }
const box = new Box(1234567890123456789n); console.log("class", box.read());
function collect(...items: bigint[]): bigint {return items.reduce((sum, value) => sum + value, 0n);}
console.log("rest", collect(...values));
const [first, ...remaining] = values; console.log("destructure", first, remaining.join(","));
const map = new Map([[1n, 2n], [3n, 4n]]); console.log("map", map.get(BigInt("1")), [...map.values()].join(","));
Promise.all(values).then(result => console.log("promise", result.join(",")));
async function sum(): Promise<bigint> {let total = 0n; for await (const value of values) total += value; return total;}
sum().then(value => console.log("async", value));
