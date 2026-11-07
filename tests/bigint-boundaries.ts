let seed = 123456789012345678901234567890n;
const mask = (1n << 192n) - 1n;
function next(): bigint {
    seed = (seed * 6364136223846793005n + 1442695040888963407n) & mask;
    return (seed & 1n) === 0n ? seed : -seed;
}
for (let index = 0; index < 96; index++) {
    const left = next(), right = next() || 1n;
    const shift = BigInt(index % 65 - 32);
    console.log("pair", index, left + right, left - right, left * right, left / right, left % right);
    console.log("bits", left & right, left | right, left ^ right, ~left, left << shift, left >> shift);
    for (const width of [0, 1, 7, 8, 31, 32, 63, 64, 127, 128, 191, 192, 256]) {
        console.log("width", width, BigInt.asIntN(width, left), BigInt.asUintN(width, left));
    }
}
for (const integer of [-9007199254740993n, -9007199254740992n, -1n, 0n, 1n, 9007199254740992n, 9007199254740993n]) {
    for (const number of [-Infinity, -1e100, -9007199254740992, -1.5, -1, -0.5, 0, 0.5, 1, 1.5, 9007199254740992, 1e100, Infinity, NaN]) {
        console.log("compare", integer, number, integer < number, integer <= number, integer > number, integer >= number, (integer as any) == number);
    }
}
