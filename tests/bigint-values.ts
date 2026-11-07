const first = 9007199254740993123456789n;
console.log("value", typeof first, String(first), first.toString(), first === BigInt("9007199254740993123456789"));
for (const value of [0n, 1n, 1234567890123456789n, 0xffn, 0b101n, 0o77n, 1_000n]) console.log("literal", String(value), Boolean(value), value.toString(16));
for (const value of ["", "   ", "-123", "+123", "0xFF", "0b101", "0o77", true, false, 123, 1e20]) console.log("convert", String(BigInt(value)));
const boxed = Object(first);
console.log("boxed", boxed.valueOf() === first, String(boxed), Object.prototype.toString.call(first), Object.prototype.toString.call(boxed));
console.log("number", Number(123n), Number(Object(123n)));
const values = new Set([123n, BigInt("123"), 124n]);
console.log("set", values.size, values.has(BigInt("123")));
const map = new Map([[123n, "found"]]); console.log("map", map.get(BigInt("123")));
const object: any = {}; object[first as any] = 7; console.log("key", Object.keys(object)[0], object[first as any]);
for (const value of [255n, 256n, BigInt("-1"), BigInt("-129")]) console.log("width", String(BigInt.asIntN(8, value)), String(BigInt.asUintN(8, value)));
for (const value of [null, undefined, 1.5, Infinity, "1.2", "0x", "12n", "hello"]) try {BigInt(value as any);} catch (error) {console.log("error", (error as Error).name);}
try {new (BigInt as any)(1);} catch (error) {console.log("construct", (error as Error).name);}
try {JSON.stringify(first);} catch (error) {console.log("json", (error as Error).name);}
console.log("metadata", BigInt.name, BigInt.length, BigInt.asIntN.length, BigInt.prototype.toString.length);
console.log("negative", String(-123n), String(-(-123n)), String(1n + 2n));
console.log("equal", (123n as any) == 123, (123n as any) == "123", (0n as any) == false, (1n as any) == true, (1n as any) == "bad");
console.log("compare", 9007199254740993n > 9007199254740992, 123n < 123.5, -123n > -123.5, 123n <= 123, 123n > ("bad" as any));
console.log("console", first, -123n, 0n);
console.log("template", `${first}`, [1n, 2n].join(","));
console.log("boxed equal", (Object(123n) as any) == 123n);
