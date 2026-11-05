const numbers: number[] = [10, 20, 30, 40];
const mixed: any[] = [10, "twenty", undefined, null];
for (let i = 0; i < 12; i++) {
    console.log(numbers[i % 4], mixed[i & 3], Object.hasOwn(mixed, i % 4));
    numbers[i % 4] += 1;
    mixed[i & 3] = i;
}
console.log(numbers[0], numbers[3], mixed[0], mixed[3]);
const mask = 3;
let unchanged = 2;
console.log(numbers[unchanged], numbers[-1 & mask], numbers[(7 & 3) ^ 1]);
console.log(numbers[(7 & 1) | 2], numbers[(4294967295 & 3)]);
const dynamic: any = numbers;
dynamic[-1] = 77;
dynamic["NaN"] = 88;
dynamic["1.5"] = 99;
console.log(numbers[-1 | 0], numbers[-1 % 4], numbers[1.5 % 4], numbers[0 % 0]);
console.log(numbers[(2147483648 | 0)], numbers[2147483648 >>> 0]);
let index = 0;
function advance() { index++; return index; }
console.log(numbers[advance() & 3], index);
numbers[advance() & 3] += advance();
console.log(numbers[2], index);
const original: number[] = [1, 2, 3, 4];
let chosen = original;
function replace() { chosen = [11, 12, 13, 14]; return 1; }
console.log(chosen[replace() & 3], original[1], chosen[1]);
function arithmetic(count: number) {
    let total = 0;
    for (let i = 1; i < count; i++) {
        total += numbers[i % 4];
        if (i === 5) break;
    }
    return total;
}
console.log(arithmetic(4.5), arithmetic(2147483648));
