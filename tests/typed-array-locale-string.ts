/// <reference lib="es2024.arraybuffer" />
export {};
const numbers = new Float64Array([1234.5, -2.25]);
console.log('number', numbers.toLocaleString('en-US', {useGrouping: false, maximumFractionDigits: 1}));
console.log('empty', new Uint8Array(0).toLocaleString());
const big = new BigInt64Array([1234n, -2n]);
console.log('bigint', big.toLocaleString('en-US', {useGrouping: false}));
Object.defineProperty(numbers, 'length', {get() {throw new Error('public length');}});
console.log('internal', numbers.toLocaleString('en-US', {useGrouping: false}));
const hidden = new Uint8Array([4, 5]);
Object.setPrototypeOf(hidden, null);
console.log('hidden', Uint8Array.prototype.toLocaleString.call(hidden, 'en-US'));
const original = Number.prototype.toLocaleString;
let calls = 0;
Number.prototype.toLocaleString = function(locales?: any, options?: any): string {calls++; return String(this.valueOf()) + ':' + locales + ':' + options.label;};
try {console.log('custom', new Uint8Array([1, 2]).toLocaleString('custom', {label: 'option'} as any), calls);}
finally {Number.prototype.toLocaleString = original;}
const detached = new Uint8Array([1, 2, 3]);
calls = 0;
Number.prototype.toLocaleString = function(): string {calls++; if (calls === 1) detached.buffer.transfer(); return String(this.valueOf());};
try {console.log('detached', detached.toLocaleString(), calls);}
catch (error) {console.log('detached error', (error as Error).name, calls);}
finally {Number.prototype.toLocaleString = original;}
