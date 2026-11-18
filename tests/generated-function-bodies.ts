export {};

function create(offset: number) {
    return function read(a: number, b: number, c: number, d: number): number {return offset + a + b + c + d;};
}
const first = create(10), second = create(20);
const source = first.toString();
console.log('generic', first(1, 2, 3, 4), second(1, 2, 3, 4));
console.log('calls', first.call(null, 2, 3, 4, 5), first.apply(null, [3, 4, 5, 6]));
console.log('identity', first !== second, first.name, first.length, source === second.toString());
console.log('prototype', first.prototype !== second.prototype, first(4, 5, 6, 7), first.toString() === source);
console.log('own names', Object.getOwnPropertyNames(first).join(','));
const bound = first.bind(null, 1, 2);
console.log('bound', bound(3, 4), bound.name, bound.length);

function variable(prefix: string, ...values: number[]): string {return prefix + ':' + values.join(',');}
const generic = variable;
console.log('rest', generic('items', 1, 2, 3), generic.call(null, 'call', 4, 5));
function defaults(a = 2, b = 3, c = 4, d = 5): number {return a + b + c + d;}
const defaultCall = defaults;
console.log('defaults', defaultCall(), defaultCall.apply(null, [undefined, 7, undefined, 9]));

const Constructor: any = function (this: any, a: number, b: number, c: number, d: number): void {this.value = a + b + c + d;};
console.log('constructed', new Constructor(1, 2, 3, 4).value, new (Constructor.bind(null, 2, 3))(4, 5).value);
const argumentsCall = function (this: {value: number}, ...values: number[]): string {
    return this.value + ':' + arguments.length + ':' + values.join(',');
};
console.log('arguments', argumentsCall.call({value: 8}, 1, 2, 3), argumentsCall.apply({value: 9}, [4, 5]));

function capture<T>(value: T): () => T {return () => value;}
console.log('generic zero arguments', capture(42)(), capture('text')());
function identity<T>(value: T): T {return value;}
function getIdentity() {return identity;}
const selected = getIdentity()<number>;
console.log('generic one argument', selected(7));
function choose<T>(first: T, second: T): T {return first;}
function getChoice() {return choose;}
const choice = getChoice()<number>;
console.log('generic two arguments', choice(8, 9));
