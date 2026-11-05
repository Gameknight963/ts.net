function next(state: {calls: number}) { state.calls++; return 7; }
function cases(value: any) {
    const state = {calls: 0};
    let a = value;const first = (a &&= next(state));
    let b = value;const second = (b ||= next(state));
    let c = value;const third = (c ??= next(state));
    console.log(first, a, second, b, third, c, state.calls);
}
cases(undefined);cases(null);cases(false);cases(0);cases(-0);cases(0 / 0);cases("");cases(3);cases("hello");
let number: number | undefined;
console.log(number &&= 9, number);
console.log(number ??= 4, number);
console.log(number ||= 8, number);
let flag: boolean | undefined;
console.log(flag &&= true, flag ??= false, flag ||= true, flag);
let text: string | undefined;
console.log(text ??= "hello", text &&= "next");
const root = {value: undefined as number | undefined};
console.log(root.value ??= 5, root.value ||= 9, root.value &&= 7, root.value);
const values = [0, -0, 0 / 0, 2];
for (let i = 0; i < 4; i++) values[i] ||= i + 1;
console.log(values[0], values[1], values[2], values[3]);
for (let i = 0; i < 4; i++) values[i] &&= undefined as unknown as number;
console.log(values[0], values[1], values[2], values[3]);
const state = {calls: 0};
function receiver(items: any[], state: {calls: number}) { state.calls++; return items; }
function key(state: {calls: number}) { state.calls++; return 0; }
const items: any[] = [0];
console.log(receiver(items, state)[key(state)] ||= next(state), state.calls);
console.log(receiver(items, state)[key(state)] ??= next(state), state.calls);
let source = {value: 0};
const original = source;
function replace() { source = {value: 20}; return 9; }
console.log(source.value ||= replace(), original.value, source.value);
let union: string | number = "";
console.log(union ||= 4, union);
let missing: any;
function nothing(): void {}
console.log(missing ??= nothing(), missing);
var globalValue: number | undefined;
function globalAssignments() { for (let i = 0; i < 3; i++) globalValue ??= i + 1; return globalValue; }
console.log(globalAssignments(), globalValue);
const readOnly: any = "a";
console.log(readOnly.length ||= 7, readOnly.length ??= 7, readOnly[0] ||= "b");
const emptyString: any = "";
console.log(emptyString.length &&= 7);
let array = [0];const oldArray = array;
function replaceArray() { array = [20]; return 9; }
console.log(array[0] ||= replaceArray(), oldArray[0], array[0]);
let optional: number | undefined = 3;
console.log(optional &&= undefined, optional);
let zero: number | undefined = 0;
console.log(zero ||= undefined, zero);
