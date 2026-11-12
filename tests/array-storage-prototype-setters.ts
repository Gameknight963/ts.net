export {};

const values: any[] = [1, 'two', 3];
if (typeof values[0] !== 'number') throw new Error('wrong initial value');
let calls = 0, observed: unknown;
Object.defineProperty(Array.prototype, '0', {
    get() {return 99;},
    set(value: unknown) {calls++;},
    configurable: true
});
try {values[1000] = 5; observed = values[0];}
finally {delete (Array.prototype as any)[0];}
console.log('internal copy', observed, calls, values[1000], values.length);

const readonly: any[] = [2, 'text'];
if (typeof readonly[0] !== 'number') throw new Error('wrong readonly initial value');
let result = '';
Object.defineProperty(Array.prototype, '0', {value: 98, writable: false, configurable: true});
try {
    readonly[1000] = 6;
    result = readonly[0] + ':' + readonly[1] + ':' + readonly[1000];
} catch {result = 'internal copy threw';}
finally {delete (Array.prototype as any)[0];}
console.log('readonly prototype copy', result, readonly.length);
