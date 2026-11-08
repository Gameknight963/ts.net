export {};
const values: any = [1, 2];
Object.defineProperty(values, 'label', {value: 3, writable: true, enumerable: true, configurable: true});
values.label = 4;
console.log('data', values.label, Reflect.set(values, 'label', 5), values.label, values.length, Object.keys(values).join(','));
Object.defineProperty(values, 'hidden', {value: 6, configurable: true});
try {values.hidden = 7;} catch (error) {console.log('readonly', (error as Error).name);}
console.log('names', Object.getOwnPropertyNames(values).join(','), Object.keys(values).length);
console.log('delete readonly', Reflect.deleteProperty(values, 'hidden'), Object.hasOwn(values, 'hidden'));
let stored = 8;
function get() {return stored;}
function set(value: number) {stored = value;}
Object.defineProperty(values, 'label', {get, set, enumerable: false});
values.label = 9;
console.log('accessor', values.label, Object.getOwnPropertyDescriptor(values, 'label')!.get === get, values.__lookupGetter__('label') === get, Object.keys(values).join(','));
Object.defineProperty(values, 'label', {value: null, writable: true, enumerable: true});
console.log('transition', values.label, values.__lookupGetter__('label') === undefined, Reflect.set(values, 'label', 10), values.label);
Object.defineProperty(values, 'nullable', {get() {return null;}});
console.log('null getter', values.nullable === null);
Object.defineProperty(values, 'fixed', {value: NaN});
console.log('fixed', Reflect.defineProperty(values, 'fixed', {value: NaN}), Reflect.defineProperty(values, 'fixed', {value: 1}), Reflect.deleteProperty(values, 'fixed'));
const symbol = Symbol('field');
Object.defineProperty(values, symbol, {value: 11, enumerable: true});
console.log('symbol', values[symbol], Object.getOwnPropertySymbols(values).length, Object.keys(values).join(','));
const parent: any[] = [];
Object.defineProperty(parent, 'label', {value: 12, writable: true, configurable: true});
const child: any[] = [];
Object.setPrototypeOf(child, parent);
(child as any).label = 13;
console.log('inherited data', (child as any).label, (parent as any).label, Object.hasOwn(child, 'label'));
console.log('spread', JSON.stringify({...values}), Object.getOwnPropertyDescriptor({...values}, 'label')!.writable);
const receiver: any[] = [];
console.log('reflect receiver', Reflect.set(parent, 'label', 14, receiver), (receiver as any).label, (parent as any).label);
Object.defineProperty(Array.prototype, 'map', {enumerable: true});
console.log('prototype enumeration', Object.keys(Array.prototype).join(','), Object.getOwnPropertyNames(Array.prototype).includes('push'));
Object.defineProperty(Array.prototype, 'map', {enumerable: false});
console.log('prototype hidden', Object.keys(Array.prototype).length, Object.getOwnPropertyNames(Array.prototype).includes('map'));
