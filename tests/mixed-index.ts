const values: any[] = [1, "two", , undefined, null];
for (let i = 0; i < 7; i++) console.log(values[i], typeof values[i]);
for (let i = 0; i < 3; i++) values[i] = i + 10;
for (let i = 0; i < 3; i++) console.log(values[i]++, values[i] += 2, values[i]);
const strings = ["a", "b"];
for (let i = 0; i < 2; i++) { strings[i] += "x"; console.log(strings[i]); }
const numbers = [1, 2];
const wide: any[] = numbers;
wide[0] += 2;
console.log(wide[0], numbers[0]);
const named: any = values;
named[-1] = "negative";
named[1.5] = "fraction";
console.log(named[-1], named[1.5]);
values[2147483647] = "large";
values[4294967294] = "last";
console.log(values[2147483647], values[4294967294], values.length);
let selected: any[] = [1];
const original = selected;
function replace() { selected = [20]; return 3; }
console.log(selected[0] += replace(), original[0], selected[0]);
const target: any[] = [];
for (target[0] of [1, 2, 3]) console.log(target[0]);
