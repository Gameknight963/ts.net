export {};
class Item {
    base = 2;
    get direct(): number {return this.base;}
    get computed(): number {return this.base * 2 + 1;}
    get negative(): number {return -this.base;}
}
function consume(value: Item): number {return value.direct + 1 + value.computed * 2;}
function multiply(value: Item): number {return value.direct * 1;}
function negate(value: Item): number {return value.negative * 1;}
function preserve(value: Item): number {return value.direct;}
const item = new Item();
console.log('initial', consume(item));
item.base = undefined as any;
console.log('undefined', preserve(item), consume(item), Number.isNaN(multiply(item)));
item.base = -0;
console.log('negative zero', Object.is(multiply(item), -0), Object.is(negate(item), 0));
item.base = 0; console.log('computed negative zero', Object.is(negate(item), -0));
item.base = 3;
const direct = Object.getOwnPropertyDescriptor(Item.prototype, 'direct')!.get!;
const computed = Object.getOwnPropertyDescriptor(Item.prototype, 'computed')!.get!;
Object.defineProperty(Item.prototype, 'direct', {get() {return 17;}, configurable: true});
console.log('changed getter', consume(item));
Object.defineProperty(Item.prototype, 'direct', {get: direct, configurable: true});
Object.defineProperty(item, 'computed', {value: 29, configurable: true});
console.log('own shadow', consume(item));
delete (item as any).computed;
console.log('remove shadow', consume(item));
let reads = 0;
Object.defineProperty(item, 'base', {get() {return ++reads;}, configurable: true});
console.log('field side effects', consume(item), reads);
Object.defineProperty(item, 'base', {get() {
    Object.defineProperty(Item.prototype, 'computed', {get() {return 101;}, configurable: true});
    return 5;
}, configurable: true});
console.log('replace during read', consume(item));
Object.defineProperty(Item.prototype, 'computed', {get: computed, configurable: true});
Object.defineProperty(item, 'base', {get() {throw new Error('field error');}, configurable: true});
try {console.log(consume(item));} catch (error) {console.log((error as Error).message);}
Object.defineProperty(item, 'base', {value: 7, writable: true, configurable: true});
let selections = 0;
function next(): Item {selections++; return item;}
console.log('single receiver evaluation', next().computed * 2, selections);
const structural: Item = {base: 3, direct: 4, computed: 5, negative: -0};
console.log('structural', consume(structural));
class Child extends Item {get direct(): number {return 31;}}
console.log('derived', consume(new Child()));
