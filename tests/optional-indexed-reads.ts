export {};
type Callback = (this: unknown, value: number) => string;
let keys = 0, argumentsEvaluated = 0;
const values: Callback[] = [function(this: unknown, value) {return `${this === values}:${value}`;}];
function key(): number {keys++; return 0;}
function argument(): number {argumentsEvaluated++; return 7;}
function invoke(items: Callback[] | null | undefined): string | undefined {
    return items?.[key()]?.(argument());
}
console.log('missing base', invoke(null), invoke(undefined), keys, argumentsEvaluated);
console.log('missing element', invoke([]), keys, argumentsEvaluated);
console.log('present', invoke(values), keys, argumentsEvaluated);
function read(items: Callback[] | null, index: number): Callback | undefined {return items?.[index];}
console.log('identity', read(values, 0) === values[0], read(null, 0));
const events: string[] = [];
const observed: Callback[] = [];
Object.defineProperty(observed, '0', {get() {
    events.push(`get:${this === observed}`);
    return function(this: unknown, value: number) {return `old:${this === observed}:${value}`;};
}, configurable: true});
function replace(): number {
    events.push('argument');
    Object.defineProperty(observed, '0', {value: () => 'new', configurable: true});
    return 8;
}
console.log('capture', observed?.[Number('0')]?.(replace()), events.join(','));
console.log('replacement', observed?.[Number('0')]?.(9));
const inherited: Callback[] = new Array(1);
const parent = Object.create(Array.prototype);
parent[0] = function(this: unknown, value: number) {return `${this === inherited}:${value}`;};
Object.setPrototypeOf(inherited, parent);
console.log('inherited', inherited?.[Number('0')]?.(10));
const proxy = new Proxy(values, {get(target, key, receiver) {
    events.push(`proxy:${String(key)}`); return Reflect.get(target, key, receiver);
}});
events.length = 0;
console.log('proxy', proxy?.[Number('0')]?.(11), events.join(','));
const special: Callback[] = [];
for (const index of [1.5, -1, Infinity, -Infinity, NaN, 4294967295, 2147483648]) {
    (special as any)[index] = function(this: unknown, value: number) {return `${this === special}:${value}`;};
    console.log('key', index, special?.[index]?.(12));
}
(special as any).undefined = () => 'undefined key';
console.log('undefined', special?.[undefined as unknown as number]?.(13));
function unknownRead(value: any, index: number): any {return value?.[index];}
console.log('other values', unknownRead('abc', 1), unknownRead({1: 'object'}, 1),
    unknownRead(new Uint8Array([4, 5]), 1), unknownRead(undefined, 1));
const numbers: (number | undefined)[] = [0, NaN, undefined];
for (const index of [0, 1, 2, 3]) console.log('numbers', numbers?.[index]);
function readArgument(index: number, ...rest: string[]): any {return arguments?.[index];}
console.log('arguments', readArgument(1, 'first'));
