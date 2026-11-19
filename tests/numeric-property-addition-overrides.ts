export {};
function add(target: any, value: number): number {return target.base + value;}
const record = {base: 2}; let reads = 0;
Object.defineProperty(record, 'base', {get() {reads++; return 7;}, configurable: true});
console.log(add(record, 3), reads);
Object.defineProperty(record, 'base', {value: 11, writable: false});
console.log(add(record, 3));
const inherited = Object.create({get base() {reads++; return 13;}});
console.log(add(inherited, 3), reads);
const proxy = new Proxy({base: 17}, {get(target, key, receiver) {reads++; return Reflect.get(target, key, receiver);}});
console.log(add(proxy, 3), reads);
console.log(add(Object.freeze({base: 19}), 3));
console.log(add({base: {valueOf() {reads++; return 23;}}}, 3), reads);
console.log(add({base: 29}, undefined!));
