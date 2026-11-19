export {};
function add(value: number, target: any): number {return value + target.e;}
function opposite(target: any, value: number): number {return target.e + value;}
function total(target: any): number {let sum = 0; for (let i = 0; i < 3; i++) sum += target.e; return sum;}
const target = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"e":1000.5}');
console.log(add(2, target), opposite(target, 2), total(target));
target.e = undefined;
console.log(add(2, target), opposite(target, 2), total(target), Object.hasOwn(target, 'e'));
target.e = 11;
let reads = 0;
Object.setPrototypeOf(target, {get e() {reads++; return 13;}});
console.log(add(2, target), reads);
delete target.e;
console.log(add(2, target), opposite(target, 2), total(target), reads);
Object.defineProperty(target, 'e', {value: 7, writable: false});
console.log(add(2, target), total(target));
const mixed = JSON.parse('{"a":1,"e":true}');
console.log(add(2, mixed), opposite(mixed, 2), total(mixed));
mixed.e = null;
console.log(add(2, mixed), total(mixed));
mixed.e = {valueOf() {reads++; return 5;}};
reads = 0; console.log(add(2, mixed), opposite(mixed, 2), total(mixed), reads);
const zero = JSON.parse('{"e":-0}');
console.log(Object.is(add(-0, zero), -0), Object.is(opposite(zero, -0), -0));
const wide = JSON.parse('{' + Array.from({length:32}, (_, i) => '"p' + i + '":' + (1000.5 + i)).join(',') + '}');
function last(value: number, target: any): number {return value + target.p31;}
console.log(last(2, wide));
wide.p31 = undefined;
console.log(last(2, wide));
