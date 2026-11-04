type Counter = { value: number };
function make(value: number): Counter { return { value: value }; }
function touch(counter: Counter): Counter { counter.value++; return counter; }
function parameter(value: number): number { value += 2; return value++; }
const counter = make(3);
console.log(counter.value++, counter.value, counter.value += 2, counter.value);
console.log(touch(counter).value++, counter.value, parameter(5));
const nan = 0 / 0;
console.log(nan < 1, nan > 1, nan <= 1, nan >= 1, nan === nan, nan !== nan);
console.log(false && (counter.value++ > 0), true || (counter.value++ > 0), counter.value);
const values: any = { flag: true, text: "hello" };
values.flag = false;
values.text = "updated";
console.log(values.flag, values.text);
function many(a: number, b: number, c: number, d: number, e: number): number {
    return a + b * c - d / e;
}
console.log(many(1, 2, 3, 4, 5));
