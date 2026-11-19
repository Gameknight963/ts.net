export {};
function add(value: number, target: any): number {return value + target.length;}
function opposite(target: any, value: number): number {return target.length + value;}
function total(target: any): number {let sum = 0; for (let i = 0; i < 3; i++) sum += target.length; return sum;}
for (const target of [[], [1, 2], [1, 'text'], JSON.parse('[{"a":1},{"a":2}]'), new Array(7), Object.freeze([1, 2, 3])])
    console.log(add(2, target), opposite(target, 2), total(target));
const sparse: any[] = [];
sparse.length = 4294967295;
console.log(add(2, sparse), total(sparse));
let reads = 0;
const proxy = new Proxy([1, 2], {get(target, key, receiver) {reads++; return Reflect.get(target, key, receiver);}});
console.log(add(2, proxy), opposite(proxy, 2), total(proxy), reads);
const like = {length: 3};
Object.defineProperty(like, 'length', {get() {reads++; return 9;}});
reads = 0; console.log(add(2, like), opposite(like, 2), total(like), reads);
function moving(target: any): number {return target.length + (() => {target.push(9); return 3;})();}
const changing = [1, 2];
console.log(moving(changing), changing.length);

function leftFirst(target: any): number {
    function value(): number {target.push(7); return 2;}
    return value() + target.length;
}
console.log(leftFirst([1, 2]));
const conversion: any = {length: {valueOf() {reads++; return 4;}}};
reads = 0; console.log(add(2, conversion), reads);
