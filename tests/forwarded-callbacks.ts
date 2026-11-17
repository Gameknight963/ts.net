export {};
type Item = { count: number };
type Callback = (item: Item, index: number) => number;
function add(item: Item, index: number): number { return item.count + index; }
function forward(callback: Callback, item: Item, index: number): number { return callback(item, index); }
function reordered(item: Item, callback: Callback, index: number): number { return callback(item, index); }
function swapped(callback: (index: number, item: Item) => number, item: Item, index: number): number { return callback(index, item); }
function reverse(index: number, item: Item): number { return item.count + index; }
console.log('basic', forward(add, { count: 7 }, 3), reordered({ count: 8 }, add, 4), swapped(reverse, { count: 9 }, 5));
function duplicate(callback: (first: number, second: number) => number, value: number): number { return callback(value, value); }
function sum(first: number, second: number): number { return first + second; }
console.log('duplicate', duplicate(sum, 6));
function empty(callback: () => number): number { return callback(); }
function constant(): number { return 11; }
console.log('empty', empty(constant));
function numeric(callback: (value: number) => number, value: number): number { return callback(value); }
function plus(value: number): number { return value + 7; }
function observe(value: any): number { return typeof value === 'number' ? value + 7 : -1; }
console.log('numeric', numeric(plus, 3), numeric(observe, 4), numeric(observe, undefined as any));
const log: string[] = [];
const item = { count: 1 };
function first(): Item { log.push('first'); return item; }
function second(): number { log.push('second'); item.count = 20; return 2; }
function last(): number { log.push('unused'); item.count = 30; return 3; }
function unused(callback: Callback, value: Item, index: number, extra: number): number { return callback(value, index); }
console.log('order', unused(add, first(), second(), last()), log.join(','));
function supplied(callback: (value: number) => number, value: number): number { return callback(value); }
function defaulted(value = 19): number { return value; }
function rest(...values: number[]): number { return values.length + values[0]; }
function argumentsCount(value: number): number { return value + arguments.length; }
console.log('fallbacks', supplied(defaulted, undefined as any), supplied(rest, 3), supplied(argumentsCount, 3));
function closure(offset: number): (value: number) => number { return value => value + offset; }
console.log('closure', supplied(closure(7), 3));
let selected: (value: number) => number = plus;
function change(): number { selected = observe; return 3; }
console.log('saved', supplied(selected, change()), selected(undefined as any));
function trigger(): number { return supplied(later, 3); }
try { trigger(); } catch (error) { console.log('tdz', (error as Error).name); }
const later = plus;
console.log('initialized', trigger());
function failure(value: number): number { throw new Error('callback:' + value); }
try { supplied(failure, 3); } catch (error) { console.log('throw', (error as Error).message); }
const proxied = new Proxy({ count: 13 }, { get(target, key, receiver) { log.push(String(key)); return Reflect.get(target, key, receiver); } });
log.length = 0;
console.log('proxy', forward(add, proxied, 2), log.join(','));
