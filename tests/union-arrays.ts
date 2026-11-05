function countNumbers(values: (number | string)[]): number {
    let total = 0;
    for (let i = 0; i < values.length; i++) {
        const value = values[i];
        if (typeof value === "number") total += value;
        else console.log(value.length);
    }
    return total;
}
const mixed: (number | string)[] = [];
for (let i = 0; i < 6; i++) mixed[i] = i % 2 === 0 ? i : "text";
console.log(countNumbers(mixed));
const alias = mixed;
alias[0] = "changed";
alias[1] = 12;
console.log(countNumbers(mixed));
console.log(mixed[20], typeof mixed[20]);
console.log(countNumbers(mixed.slice(1, 4)));
mixed.reverse();
console.log(countNumbers(mixed), mixed[0], mixed[5]);
