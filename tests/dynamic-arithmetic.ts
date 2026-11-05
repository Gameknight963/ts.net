function locals() {
    let value: any = "2";
    console.log(value++, value, ++value, value--, --value);
    value = "a";
    console.log(value += 3, value);
    value = true;
    console.log(value += 2, value *= 3, value -= 1, value /= 2, value %= 3);
    value = undefined;
    console.log(value++, value, typeof value);
    value = null;
    console.log(value--, value, ++value);
    value = 4294967295;
    console.log(value >>>= 0, value >>= 1, value <<= 2, value ^= 7, value |= 8, value &= 15);
}
locals();
function properties() {
    const point = { value: 2 };
    const view: any = point;
    console.log(view.value++, ++view.value, view.value += 2, view.value *= 3, view.value %= 5, point.value);
    view.extra = "4";
    console.log(view.extra++, view.extra, view.extra += "x");
    view.missing++;
    console.log(view.missing, typeof view.missing);
}
properties();
function arrays() {
    const array: any[] = ["2", undefined, null, true];
    console.log(array[0]++, ++array[0], array[1]++, array[2]--, array[3] += 2);
    console.log(array[0], array[1], array[2], array[3]);
    const numbers = [2, 4];
    const view: any = numbers;
    console.log(view[0]++, view[1] += 2, numbers[0], numbers[1]);
}
arrays();
function identity(value: any) { return value; }
function numbers() {
    let number = 3;
    console.log(number += identity(2), number *= identity("4"), number %= 7);
    const point = { x: 2 };
    console.log(point.x += identity(4), point.x -= identity("2"), point.x);
}
numbers();
let selected: any = ["3"];
let visits = 0;
function receiver() { visits++; return selected; }
function index() { visits += 10; return 0; }
function rhs() { selected = [20]; return 2; }
const original = selected;
console.log(receiver()[index()] += rhs(), visits, original[0], selected[0]);
console.log(receiver()[index()]++, visits, selected[0]);
let absent: any;
console.log(absent++, absent);
let negative: any = -0;
const before = negative++;
console.log(1 / before, negative);

function narrowed(value: any, extra: any) {
    if (typeof value === "number") {
        console.log(value += extra, value);
    }
}
narrowed(2, "x");
narrowed(2, 3);
function narrowedField(point: { value: number | string }, extra: any) {
    if (typeof point.value === "number") {
        console.log(point.value += extra, point.value);
    }
}
narrowedField({ value: identity(2) }, "x");

function narrowedArray(values: any[], extra: any) {
    if (typeof values[0] === "number") {
        console.log(values[0] += extra, values[0]);
    }
}
narrowedArray([identity(2)], "x");

function unknownMutation(value: unknown, extra: any) {
    if (typeof value === "number") {
        console.log(value++, value += extra, typeof value);
    }
}
unknownMutation(2, "x");
