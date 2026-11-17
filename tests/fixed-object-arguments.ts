export {};
type Item = { count: number };
function read(item: Item): number { return item.count; }
function forward(callback: (item: Item) => number, item: Item): number { return callback(item); }
const plain = { count: 41 };
function plainValue(): number { return read(plain) + forward(read, plain); }
console.log('plain', plainValue());
function early(): number { return forward(read, fixed); }
try { early(); } catch (error) { console.log('tdz', (error as Error).name); }
const fixed = { count: 7, extra: true };
console.log('initial', read(fixed), forward(read, fixed));
fixed.count = 12;
console.log('write', read(fixed), forward(read, fixed));
Object.defineProperty(fixed, 'count', { configurable: true, get() { return 19; } });
console.log('getter', read(fixed), forward(read, fixed));
delete (fixed as any).count;
console.log('deleted', read(fixed), forward(read, fixed));
Object.setPrototypeOf(fixed, { count: 23 });
console.log('prototype', read(fixed), forward(read, fixed));
let mutable: Item = { count: 3 };
function replace(): void { mutable = { count: 5 }; }
console.log('mutable', read(mutable));
replace();
console.log('replaced', forward(read, mutable));
let gets = 0;
const proxy = new Proxy({ count: 31 }, { get(target, key, receiver) { gets++; return Reflect.get(target, key, receiver); } });
console.log('proxy', forward(read, proxy), gets);
function beforeVar(): number { return read(hoisted); }
try { console.log('var', beforeVar()); } catch (error) { console.log('var', (error as Error).name); }
var hoisted = { count: 37 };
console.log('var initialized', beforeVar());
