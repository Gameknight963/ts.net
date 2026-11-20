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

const Dynamic: any = function(this: any) {this.base = 2;};
const dynamic = new Dynamic();
for (let i = 0; i < 4; i++) {dynamic.base = i; console.log('dynamic numeric', add(dynamic, 3));}
for (const value of [undefined, null, false, NaN, -0]) {
    dynamic.base = value;
    try {console.log('dynamic changed', add(dynamic, 3));} catch {console.log('dynamic changed error');}
}
Object.defineProperty(dynamic, 'base', {get() {reads++; return 31;}, configurable: true});
console.log('dynamic getter', add(dynamic, 3), reads);
delete dynamic.base;
Dynamic.prototype.base = 37;
console.log('dynamic inherited', add(dynamic, 3));
Object.defineProperty(Dynamic.prototype, 'base', {get() {reads++; return 41;}, configurable: true});
console.log('dynamic inherited getter', add(dynamic, 3), reads);
