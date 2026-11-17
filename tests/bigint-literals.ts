export {};
function literal(): bigint { return 12_345_678_901_234_567_890n; }
for (let i = 0; i < 3; i++)
    console.log('values', literal() === 12345678901234567890n, 0xffn === 255n, 0o377n === 255n, 0b1111_1111n === 255n, -0n === 0n);
console.log('arithmetic', literal() + 1n, literal() - 1n, literal() % 97n);
console.log('boxes', Object(literal()) !== Object(literal()), Object(literal()).valueOf() === literal());
{
    let calls = 0;
    const BigInt = (text: string): string => { calls++; return 'shadow:' + text; };
    console.log('shadow', 123n, BigInt('123'), calls);
}
const original = BigInt.prototype.toString;
try {
    BigInt.prototype.toString = function(): string { return 'changed'; };
    console.log('prototype', literal().toString());
} finally { BigInt.prototype.toString = original; }
console.log('restored', literal().toString());
const map = new Map<bigint, string>();
map.set(literal(), 'first'); map.set(12345678901234567890n, 'second');
console.log('map', map.size, map.get(literal()));
class Holder { value = 99n; read(): bigint { return 100n; } }
function defaulted(value = 101n): bigint { return value; }
const closure = (): bigint => 102n;
console.log('contexts', new Holder().value, new Holder().read(), defaulted(), closure());
let conversions = 0;
const input: any = { toString(): string { conversions++; return '103'; } };
console.log('conversion', BigInt(input), BigInt(input), conversions);
