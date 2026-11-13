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

class Writable {
    base = 5;
    get value(): number {return this.base;}
    set value(value: number) {this.base = value;}
}
const writable = new Writable();
writable.value = 11; console.log('setter', writable.value);
Object.defineProperty(Writable.prototype, 'value', {set(value: number) {this.base = value + 1;}});
writable.value = 17; console.log('changed setter', writable.value);
Object.freeze(writable); console.log('frozen instance', writable.value);
Object.freeze(Writable.prototype); console.log('frozen prototype', writable.value);
function makeHolder(base: number) {
    return class {
        base = base;
        get value(): number {return this.base;}
    };
}
const First = makeHolder(29), Second = makeHolder(37);
const first = new First(), second = new Second();
console.log('recreated class', first.value, second.value);
const recreatedGetter = Object.getOwnPropertyDescriptor(Second.prototype, 'value')!.get!;
Object.defineProperty(First.prototype, 'value', {get: recreatedGetter, configurable: true});
console.log('same source getter', first.value, second.value);

class Unread {
    base = 43;
    get value(): number {return this.base;}
}
const unread = new Unread();
const unreadGetter = Object.getOwnPropertyDescriptor(Unread.prototype, 'value')!.get!;
Object.defineProperty(Unread.prototype, 'value', {get() {return 53;}, configurable: true});
console.log('replaced before first read', unread.value);
Object.defineProperty(Unread.prototype, 'value', {get: unreadGetter, configurable: true});
console.log('restored after first read', unread.value);
class Shadowed {
    base = 59;
    get value(): number {return this.base;}
}
const shadowed = new Shadowed();
Object.defineProperty(shadowed, 'value', {value: 61, configurable: true});
console.log('shadow before first read', shadowed.value);
delete (shadowed as any).value;
console.log('remove first shadow', shadowed.value);
class Deleted {
    base = 67;
    get value(): number {return this.base;}
}
const deleted = new Deleted();
const deletedGetter = Object.getOwnPropertyDescriptor(Deleted.prototype, 'value')!.get!;
delete (Deleted.prototype as any).value;
console.log('deleted before first read', deleted.value);
Object.defineProperty(Deleted.prototype, 'value', {get: deletedGetter, configurable: true});
console.log('restore first deletion', deleted.value);
