export {};
const state: any = Object.create(null);
state.value = 0;
let total = 0;
for (let i = 0; i < 64; i++) {state.value = i; total += state.value;}
console.log("loop", total, state.value);
const result = (state.value = 71);
console.log("result", result, typeof result, state.value);
let order = "";
function receiver(): any {order += "receiver;"; return state;}
function number(): number {order += "number;"; return 73;}
receiver().value = number();
console.log("order", order, state.value);
let gets = 0, sets = 0, stored = 0;
Object.defineProperty(state, "value", {configurable: true,
 get() {gets++; return stored;}, set(value: number) {sets++; stored = value;}});
total = 0;
for (let i = 0; i < 64; i++) {state.value = i; total += state.value;}
console.log("accessor", total, gets, sets, stored);
delete state.value;
state.value = 79;
total = 1; total += state.value;
console.log("restored", total, state.value);
function add(value: any): any {
 const item: any = Object.create(null); item.value = value;
 let sum = 2; sum += item.value; return sum;
}
let conversions = 0;
console.log("fallback", add(true), add(null), add(undefined), add({valueOf() {conversions++; return 5;}}), conversions);
const prototype = {get value() {gets++; return stored;}, set value(value: number) {sets++; stored = value;}};
const inherited: any = Object.create(prototype);
inherited.value = 83;
total = 0; total += inherited.value;
console.log("inherited", total, gets, sets, Object.hasOwn(inherited, "value"));
const array: any = [];
array.value = 89;
total = 0; total += array.value;
console.log("array property", total);
function absent(): number | undefined {return undefined;}
const missing = (array.value = absent());
console.log("missing", missing, array.value);
order = "";
function invalid(): any {order += "receiver;"; return null;}
try {invalid().value = number();} catch (error) {console.log("failed write", order, (error as Error).name);}
const fixed: any = Object.create(null);
Object.defineProperty(fixed, "value", {value: 97, writable: false});
try {fixed.value = 101;} catch (error) {console.log("readonly", fixed.value, (error as Error).name);}

function work(): number {
 const item: any = Object.create(null);
 item.value = 0;
 let sum = 0;
 for (let i = 0; i < 100000; i++) {item.value = i; sum += item.value;}
 return sum;
}
console.log("allocation loop result", work());
