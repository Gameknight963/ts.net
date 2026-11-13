export {};
class Holder {
    base = 1;
    get value(): number {return this.base;}
    get other(): number {return 9;}
}
function read(value: Holder): number {return value.value;}
const holder = new Holder();
const original = Object.getOwnPropertyDescriptor(Holder.prototype, 'value')!.get!;
const other = Object.getOwnPropertyDescriptor(Holder.prototype, 'other')!.get!;
console.log('initial', read(holder)); holder.base = 3; console.log('changed field', read(holder));
for (const value of [undefined, NaN, -0, Infinity]) {
    holder.base = value as any;
    console.log('special', read(holder), Object.is(read(holder), -0));
}
holder.base = 3;

Object.defineProperty(Holder.prototype, 'value', {get() {return this.base + 10;}, configurable: true});
console.log('replaced', read(holder));
Object.defineProperty(Holder.prototype, 'value', {get: original, configurable: true});
console.log('restored', read(holder));
Object.defineProperty(Holder.prototype, 'value', {get: other, configurable: true});
console.log('other getter', read(holder));
Object.defineProperty(Holder.prototype, 'value', {get: original, configurable: true});
console.log('restore again', read(holder));
Object.defineProperty(holder, 'value', {value: 23, configurable: true}); console.log('own data', read(holder));
delete (holder as any).value; console.log('remove own data', read(holder));
let calls = 0;
Object.defineProperty(holder, 'value', {get() {return ++calls;}, configurable: true});
console.log('own accessor', read(holder), read(holder));
delete (holder as any).value;
Object.defineProperty(holder, 'base', {get() {return ++calls;}, configurable: true});
console.log('base accessor', read(holder), read(holder));
Object.defineProperty(holder, 'base', {value: 4, writable: true, configurable: true});
console.log('base restored', read(holder));
const prototype = Object.create(Holder.prototype);
Object.defineProperty(prototype, 'value', {get() {return 31;}, configurable: true});
Object.setPrototypeOf(holder, prototype); console.log('custom prototype', read(holder));
Object.setPrototypeOf(holder, Holder.prototype); console.log('prototype restored', read(holder));
delete (Holder.prototype as any).value; console.log('deleted', read(holder));
Object.defineProperty(Holder.prototype, 'value', {get: original, configurable: true}); console.log('readded', read(holder));
class Child extends Holder {get value(): number {return this.base + 100;}}
console.log('derived', read(new Child()));
const alias: {readonly value: number} = holder; console.log('structural', alias.value);
console.log('different receiver', Reflect.get(Holder.prototype, 'value', {base: 47, value: 99}));
class Constant {get value(): number {return 7;}}
const constant = new Constant(); console.log('constant', constant.value);
Object.defineProperty(Constant.prototype, 'value', {get() {return 13;}, configurable: true});
console.log('constant replacement', constant.value);
