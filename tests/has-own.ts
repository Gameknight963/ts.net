const point = { value: undefined, count: 2 };
console.log(Object.hasOwn(point, "value"), Object.hasOwn(point, "count"), Object.hasOwn(point, "missing"), Object.hasOwn(point, "toString"));
const view: any = point;
view.extra = undefined;
console.log(Object.hasOwn(point, "extra"), Object.hasOwn(point, "value"));
const array: any[] = [, undefined, null, false];
console.log(Object.hasOwn(array, 0), Object.hasOwn(array, 1), Object.hasOwn(array, 2), Object.hasOwn(array, 3), Object.hasOwn(array, "length"));
console.log(Object.hasOwn(array, "01"), Object.hasOwn(array, "push"), Object.hasOwn(array, 4));
array.length = 6;
array[5] = undefined;
console.log(Object.hasOwn(array, 4), Object.hasOwn(array, 5));
const copy = [...array];
const slice = array.slice();
console.log(Object.hasOwn(copy, 0), Object.hasOwn(slice, 0), Object.hasOwn(copy, 4), Object.hasOwn(slice, 4));
const numbers: (number | undefined)[] = [, undefined, 2];
console.log(Object.hasOwn(numbers, 0), Object.hasOwn(numbers, 1), Object.hasOwn(numbers, 2));
numbers.length = 1;
numbers.length = 3;
console.log(Object.hasOwn(numbers, 1), Object.hasOwn(numbers, 2));
const sparse: any[] = [];
sparse[4294967294] = undefined;
console.log(Object.hasOwn(sparse, 4294967294), Object.hasOwn(sparse, 4294967293));
console.log(Object.hasOwn("crab" as any, "length"), Object.hasOwn("crab" as any, 0), Object.hasOwn("crab" as any, -0));
console.log(Object.hasOwn("crab" as any, "01"), Object.hasOwn("crab" as any, 4), Object.hasOwn("crab" as any, "slice"));
console.log(Object.hasOwn(3 as any, "value"), Object.hasOwn(false as any, "value"));
console.log(Object.hasOwn(...[point, "value"] as const));
const args: [object, string] = [point, "missing"];
console.log(Object.hasOwn(...args));
let visits = 0;
function receiver() { visits++; return point; }
function key() { visits += 10; return "value"; }
console.log(Object.hasOwn(receiver(), key()), visits);

const numericKeys: any = { "undefined": 1, "NaN": 2, "Infinity": 3, "-1": 4, "1.5": 5, "4294967295": 6 };
function numericOwn(value: any, key?: number) { return Object.hasOwn(value, key as number); }
console.log(numericOwn(numericKeys), numericOwn(numericKeys, 0 / 0), numericOwn(numericKeys, 1 / 0));
console.log(numericOwn(numericKeys, -1), numericOwn(numericKeys, 1.5), numericOwn(numericKeys, 4294967295));
console.log(numericOwn(numbers), numericOwn(numbers, 0 / 0), numericOwn(numbers, -0));
const specialArray: any = [];
specialArray.undefined = 1;
specialArray.NaN = 2;
specialArray[4294967295] = 3;
console.log(numericOwn(specialArray), numericOwn(specialArray, 0 / 0), numericOwn(specialArray, 4294967295));
console.log(numericOwn("abc", 1), numericOwn("abc", -0), numericOwn("abc", 1.5), numericOwn("abc"));

console.log(Object.hasOwn(numericKeys, -1 | 0), Object.hasOwn(numericKeys, 1 << 0), Object.hasOwn(numericKeys, 2147483647));
const negativeIndex: any = [];
negativeIndex[-1] = undefined;
console.log(Object.hasOwn(negativeIndex, -1 | 0), Object.hasOwn(negativeIndex, 0));
console.log(Object.hasOwn("abc" as any, -1 | 0), Object.hasOwn("abc" as any, 1 << 0));

const packedPresence: (number | undefined)[] = [1, 2, 3];
console.log(Object.hasOwn(packedPresence, 0), Object.hasOwn(packedPresence, 2));
packedPresence[1] = undefined;
console.log(Object.hasOwn(packedPresence, 1), Object.hasOwn(packedPresence, 2));
packedPresence.length = 1;
packedPresence.length = 3;
console.log(Object.hasOwn(packedPresence, 0), Object.hasOwn(packedPresence, 1), Object.hasOwn(packedPresence, 2));
packedPresence[1] = 4;
packedPresence[2] = 5;
console.log(Object.hasOwn(packedPresence, 1), Object.hasOwn(packedPresence, 2));

function stringifiedKey(value: any, key?: number) { return Object.hasOwn(value, "" + key); }
function trailingKey(value: any, key?: number) { return Object.hasOwn(value, key + ""); }
console.log(stringifiedKey(numericKeys), stringifiedKey(numericKeys, 0 / 0), stringifiedKey(numericKeys, -1), stringifiedKey(numericKeys, 1.5));
console.log(trailingKey(numericKeys), trailingKey(numericKeys, 0 / 0), trailingKey(specialArray, 4294967295));
let keyVisits = 0;
function changingKey() { keyVisits++; return -0; }
console.log(Object.hasOwn(numbers, "" + changingKey()), Object.hasOwn(numbers, changingKey() + ""), keyVisits);
console.log(Object.hasOwn(numericKeys, "prefix" + 1), Object.hasOwn(numericKeys, true + ""));

const otherKeys = { prefix1: 1, "true": 2 };
console.log(Object.hasOwn(otherKeys, "prefix" + 1), Object.hasOwn(otherKeys, true + ""));
