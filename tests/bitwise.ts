function inspect(value: any, shift: any) {
    console.log(value & shift, value | shift, value ^ shift, ~value);
    console.log(value << shift, value >> shift, value >>> shift, (value | 0) === 0 && 1 / (value | 0) < 0);
}
inspect(5, 3);
inspect(-5, 1);
inspect(5.75, 1.75);
inspect(-5.75, -1);
inspect(1, 31);
inspect(1, 32);
inspect(1, 33);
inspect(-1, 0);
inspect(-0, 0);
inspect(2147483647, 1);
inspect(2147483648, 1);
inspect(4294967295, 0);
inspect(4294967296, 1);
inspect(4294967297, 1);
inspect(-4294967297, 1);
inspect(9007199254740991, 2);
inspect(1e100, 3);
inspect(1 / 0, 1);
inspect(-1 / 0, 1);
inspect(0 / 0, 0 / 0);
inspect(undefined, undefined);
inspect(null, null);
inspect(true, false);
inspect("4294967295", "33");
inspect("invalid", "");

let value = 7;
console.log(value &= 3, value |= 8, value ^= 5);
console.log(value <<= 31, value >>= 32, value >>>= 0);
const point = { value: 7 };
type Point = { value: number };
const calls = { value: 0 };
function receiver(point: Point, calls: Point) { calls.value++; return point; }
function right(point: Point) { point.value = 100; return 3; }
console.log(receiver(point, calls).value ^= right(point), point.value, calls.value);
console.log(point.value <<= 2, point.value >>= 1, point.value >>>= 31);
console.log(point.value |= 7, point.value &= 3, point.value ^= 2);

let checksum = 0;
for (let i = -40; i < 40; i++) {
    for (let shift = -35; shift < 36; shift++) {
        checksum += (i << shift) + (i >> shift) + (i >>> shift);
        checksum += ((i + 0.5) & shift) ^ ((i - 0.5) | shift);
    }
}
console.log(checksum);
