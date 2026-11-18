export {};
type Callback = (this: unknown, value: number) => string;
function invoke(values: Callback[], index: number, value: number): string {
    return values[index]!(value);
}
const events: string[] = [];
const values: Callback[] = [function(this: unknown, value) {
    return `original:${this === values}:${value}`;
}];
console.log('dense', invoke(values, -0, 1));
console.log('captured', values[Number('0')]!((values[0] = function(this: unknown, value) {
    return `replacement:${this === values}:${value}`;
}, 2)));
console.log('replacement', invoke(values, 0, 3));
const observed: Callback[] = [];
Object.defineProperty(observed, '0', {get() {
    events.push(`get:${this === observed}`);
    return function(this: unknown, value: number) {
        events.push(`call:${this === observed}:${value}`);
        return 'accessor';
    };
}});
function argument(): number {events.push('argument'); return 4;}
console.log('accessor', observed[Number('0')]!(argument()), events.join(','));
const inherited: Callback[] = new Array(1);
const parent = Object.create(Array.prototype);
Object.defineProperty(parent, '0', {get() {
    return function(this: unknown, value: number) {return `inherited:${this === inherited}:${value}`;};
}});
Object.setPrototypeOf(inherited, parent);
console.log('inherited', invoke(inherited, 0, 5));
const proxy = new Proxy(values, {get(target, key, receiver) {
    events.push(`proxy:${String(key)}`);
    return Reflect.get(target, key, receiver);
}});
events.length = 0;
console.log('proxy', invoke(proxy, 0, 6), events.join(','));
const special: Callback[] = [];
for (const key of [1.5, -1, Infinity, -Infinity, NaN, 4294967295]) {
    (special as any)[key] = function(this: unknown, value: number) {return `${this === special}:${value}`;};
    console.log('key', key, invoke(special, key, 7));
}
(special as any).undefined = function(this: unknown, value: number) {return `${this === special}:${value}`;};
console.log('undefined', invoke(special, undefined as unknown as number, 8));
special[2147483648] = function(this: unknown, value: number) {return `${this === special}:${value}`;};
console.log('sparse', invoke(special, 2147483648, 9));
function fromArguments(index: number, ...rest: Callback[]): string {
    return arguments[index](10);
}
const callback: Callback = function(this: any, value) {return `${this.length}:${value}`;};
console.log('arguments', fromArguments(1, callback));
const dictionary: {[key: number]: Callback} = {2: function(this: unknown, value) {
    return `${this === dictionary}:${value}`;
}};
const key = Number('2');
console.log('dictionary', dictionary[key]!(11));
let evaluated = 0;
try {invoke(values, 1, ++evaluated);} catch (error) {console.log('missing', (error as Error).name, evaluated);}
