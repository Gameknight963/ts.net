function remainder(value: number, divisor: number) { return value % divisor; }
function inspect(value: number, divisor: number) {
    const result = remainder(value, divisor);
    console.log(result, 1 / result, result !== result);
}
inspect(9, 2);
inspect(9, 4);
inspect(9, 3);
inspect(9.5, 2);
inspect(-9, 2);
inspect(0, 2);
inspect(-0, 2);
inspect(9, -2);
inspect(9, 0);
inspect(9, 0.5);
inspect(2147483647, 8);
inspect(2147483648, 8);
inspect(9007199254740991, 2);
inspect(1e100, 2);
inspect(1 / 0, 2);
inspect(9, 1 / 0);
inspect(0 / 0, 2);
inspect(9, 0 / 0);
let checksum = 0;
for (let i = -100; i <= 100; i++) {
    for (let divisor = 1; divisor <= 17; divisor++) {
        checksum += remainder(i, divisor);
        checksum += remainder(i + 0.25, divisor);
    }
}
console.log(checksum);
