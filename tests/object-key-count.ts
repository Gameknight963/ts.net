function kernel(count: number, seed: number): number {
    const value = {alpha: seed, beta: seed + 1};
    let total = seed;
    for (let i = 0; i < count; i++) total += Object.keys(value).length - 1;
    return total;
}
console.log(kernel(8, 3));
const keys = Object.keys;
const value: any = {x: 1, y: 2};
console.log(Object.keys(value).length, keys(value).length);
value.z = 3;
console.log(Object.keys(value).length, keys(value).length);
const array: any[] = [1, , undefined];
console.log(Object.keys(array).length, keys(array).length);
array[4294967294] = 7;
const extra: any = array; extra.extra = 3;
console.log(Object.keys(array).length, keys(array).length);
array.length = 1;
console.log(Object.keys(array).length, keys(array).length);
console.log(Object.keys("A??").length, Object.keys(3).length, Object.keys(false).length);
const fn: any = () => 3;
fn.extra = 7;
console.log(Object.keys(fn).length, keys(fn).length);
let calls = 0;
function create() { calls++; return {x: 3}; }
console.log(Object.keys(create()).length, calls);
