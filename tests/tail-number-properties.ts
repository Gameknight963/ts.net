export {};
for (const count of [5, 8]) {
    const values: {[key: string]: number} = JSON.parse('{}');
    for (let i = 0; i < count; i++) values['key' + i] = i;
    const key = 'key' + (count - 1);
    for (let i = 0; i < 20; i++) values[key] += 1;
    const copy = {...values};
    values[key] += 10;
    console.log('copy', count, values[key], copy[key], typeof (values as any)[key]);
    delete values.key0;
    delete values.key1;
    console.log('shift', Object.keys(values).join(','), Object.values(values).join(','), JSON.stringify(values));
    const shiftedCopy = {...values};
    values[key] += 10;
    console.log('shift copy', values[key], shiftedCopy[key]);
    values[key] = -0;
    console.log('zero', Object.is(values[key], -0), Object.is((values as any)[key], -0));
    values[key] = NaN;
    console.log('nan', Number.isNaN(values[key]), JSON.stringify(values));
    values[key] = undefined as any;
    console.log('undefined', values[key], Object.hasOwn(values, key));
    (values as any)[key] = 'text';
    console.log('text', (values as any)[key]);
    values[key] = 17;
    Object.freeze(values);
    let rejected = false;
    try {values[key]++;} catch {rejected = true;}
    console.log('frozen', rejected, values[key]);
}
const symbol = Symbol('number');
const symbolic: any = JSON.parse('{"a":0,"b":0,"c":0,"d":0,"e":0,"f":0}');
symbolic[symbol] = 0;
function update(value: {[key: symbol]: number}, key: symbol): void { value[key] += 1; }
for (let i = 0; i < 20; i++) update(symbolic, symbol);
console.log('symbol', symbolic[symbol], Object.getOwnPropertySymbols(symbolic)[0] === symbol);
export function work(count: number): number {
    const values: {[key: string]: number} = JSON.parse('{"a":0,"b":0,"c":0,"d":0,"e":0,"f":0,"g":0,"h":0}');
    let key = 'h';
    for (let i = 0; i < count; i++) values[key] += 1;
    return values[key];
}
console.log('updates', work(10));
const accessor: {[key: string]: number} = JSON.parse('{"h":0}');
let stored = 2;
const events: string[] = [];
Object.defineProperty(accessor, 'h', {get() {events.push('get'); return stored;}, set(value: number) {events.push('set:' + value); stored = value;}});
let selectedKey = 'h';
console.log('accessor', accessor[selectedKey]++, ++accessor[selectedKey], accessor[selectedKey] += 3, events.join(','));
let gets = 0, sets = 0;
const proxied = new Proxy(accessor, {get(target, key, receiver) {gets++; return Reflect.get(target, key, receiver);}, set(target, key, value, receiver) {sets++; return Reflect.set(target, key, value, receiver);}});
proxied[selectedKey] += 2;
console.log('proxy', stored, gets, sets);
const array = [1, 2];
const arrayRecord: {[key: string]: number} = array as any;
arrayRecord['0'] += 3;
console.log('array', array.join(','));
Object.freeze(array);
let arrayRejected = false;
try { arrayRecord['1'] += 1; } catch { arrayRejected = true; }
console.log('array frozen', arrayRejected, array[1]);
const typed = new Float64Array([1, 2]);
const typedRecord: {[key: string]: number} = typed as any;
typedRecord['0'] += 3;
let numericKey: any = -0;
typedRecord[numericKey] += 2;
typedRecord['-0'] = 99;
console.log('typed', typed[0], typed[1]);
