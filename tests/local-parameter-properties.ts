export {};
class Holder {constructor(public value: number) {}}
function kernel(count: number): number {let total = 0; for (let i = 0; i < count; i++) total += new Holder(i).value; return total;}
function escape(): Holder {return new Holder(7);}
console.log('kernel', kernel(10), escape().value, escape() instanceof Holder);
class Mixed {extra = 3; constructor(public text: string, public enabled: boolean, public value: number) {this.value = value;}}
function mixed(): string {const value = new Mixed('hello', true, 11); return value.text + ':' + value.enabled + ':' + (value.value + value.extra);}
console.log('mixed', mixed());
const trace: string[] = [];
function argument(name: string, value: number): number {trace.push(name); return value;}
class Pair {constructor(public first: number, public second: number) {this.first = second;}}
function order(): string {const pair = new Pair(argument('first', 13), argument('second', 17)); return pair.first + ':' + pair.second;}
console.log('order', order(), trace.join(','));
Object.defineProperty(Holder.prototype, 'value', {set(value: number) {throw new Error('own definition must win');}, configurable: true});
console.log('own definition', kernel(3));
class Default {constructor(public value = 19) {}}
class Effect {constructor(public value: number) {trace.push('effect');}}
console.log('fallback', new Default().value, new Effect(23).value, trace.join(','));
function before(): number {return new Later(argument('late', 29)).value;}
const length = trace.length;
try {before();} catch {console.log('tdz', trace.length === length);}
class Later {constructor(public value: number) {}}
console.log('ready', before());
class Readonly {constructor(public readonly value: number, private label: string) {}}
console.log('readonly', new Readonly(31, 'private').value);
class Declared {declare value: number; constructor(value: number) {this.value = value;}}
let setterValue = 0;
Object.defineProperty(Declared.prototype, 'value', {set(value: number) {setterValue = value;}, get() {return setterValue;}, configurable: true});
function declared(): number {const value = new Declared(37); return value.value;}
console.log('inherited setter', declared(), setterValue);
