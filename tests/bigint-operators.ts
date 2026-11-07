const a = 9007199254740993123456789n, b = 123n;
console.log("arithmetic", a - b, a * b, a / b, a % b, 3n ** 20n);
console.log("signed", -17n / 3n, -17n % 3n, 17n / -3n, 17n % -3n);
console.log("bits", 123n & 31n, 123n | 256n, 123n ^ 31n, ~123n, ~(-1n));
console.log("shifts", 1n << 80n, -123n >> 3n, 128n << -2n, 3n >> -4n, -1n >> 999999999999999999n);
let value = 10n;
console.log("updates", value++, value, ++value, value--, --value);
value += 3n; value *= 5n; value -= 2n; value /= 3n; value %= 9n; value **= 3n;
console.log("compound", value);
value |= 128n; value &= 255n; value ^= 7n; value <<= 2n; value >>= 1n;
console.log("bit compound", value);
const object = {value: 1n}; console.log("property", object.value++, ++object.value, object.value);
const array = [4n]; console.log("index", array[0]++, array[0]);
let calls = ""; let stored = 7n;
const accessor = {get value() {calls += "G"; return stored;}, set value(value: bigint) {calls += "S"; stored = value;}};
console.log("accessor", accessor.value++, stored, calls);
const dynamic: any = 12n; console.log("any", dynamic * 2n, dynamic / 5n, dynamic >> 1n, ~dynamic);
let update: any = 9n; console.log("any update", update++, update, --update);
for (const operation of [() => 1n / 0n, () => 1n % 0n, () => 2n ** -1n, () => (1n as any) * 1, () => (1n as any) >>> (1n as any), () => +(1n as any)]) {
    try { operation(); } catch (error) {console.log("error", (error as Error).name);}
}
console.log("large power", 0n ** 999999999999999999n, 1n ** 999999999999999999n, (-1n) ** 999999999999999999n);
const derived = dynamic * 3n; console.log("inferred any", derived);
const other: any = 3n; const inferred = dynamic * other; console.log("both any", inferred);
