let order = '';
const source = {
    get length() { order += 'length;'; return 2; },
    get 0() { order += 'get0;'; return { valueOf() { order += 'convert0;'; return 4; } }; },
    get 1() { order += 'get1;'; return { valueOf() { order += 'convert1;'; return 5; } }; }
};
class Result extends Uint8Array { constructor(length: number) { order += 'constructor;'; super(length); } }
const result = Result.from(source as any, (value: any, index) => { order += 'map' + index + ';'; return value; });
console.log('from', result.join(','), order);
order = '';
console.log('construct', new Uint8Array(source as any).join(','), order);
const array = [1,2];
(array as any)[Symbol.iterator] = null;
console.log('snapshot', Uint8Array.from(array, (value,index) => { if(index===0) array.push(3); return value; }).join(','));
let reads = 0;
const overridden = [6,7];
const prototype = { get [Symbol.iterator]() { reads++; return undefined; } };
Object.setPrototypeOf(prototype, Array.prototype); Object.setPrototypeOf(overridden, prototype);
console.log('disabled', new Uint8Array(overridden).join(','), reads);
reads = 0;
console.log('disabled from', Uint8Array.from(overridden).join(','), reads);
order = '';
const input = { get [Symbol.iterator]() { order += 'iterator;'; return [1][Symbol.iterator]; } };
try { Uint8Array.from.call({} as any, input as any); }
catch(error) { console.log('receiver', (error as Error).name, order); }
const boxed = new String('12');
(boxed as any)[Symbol.iterator] = function*() { yield 8; };
console.log('boxed', Uint8Array.from(boxed as any).join(','), new Uint8Array(boxed as any).join(','));

order = '';
try { Uint8Array.from(source as any, () => { order += 'throw;'; throw 'mapping failed'; }); }
catch(error) { console.log('short circuit', error, order); }
order = '';
const iterable = { *[Symbol.iterator]() { order += 'yield0;'; yield 1; order += 'yield1;'; yield 2; } };
console.log('iterable', Result.from(iterable, value => { order += 'map;'; return value; }).join(','), order);
