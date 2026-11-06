function joined(values: any): string { let text = ''; for (const value of values) text += (text ? ',' : '') + value; return text; }
function inspect(first: number, second?: string): IArguments {
    console.log('initial', arguments.length, arguments[0], arguments[1], arguments[2], arguments === arguments);
    first = 20;
    console.log('parameter', first, arguments[0]);
    arguments[0] = 30;
    console.log('argument', first, arguments[0]);
    arguments[5] = 'extra';
    console.log('length', arguments.length, arguments[5]);
    console.log('own', Object.hasOwn(arguments, 'length'), Object.hasOwn(arguments, 'callee'), 'callee' in arguments);
    console.log('keys', joined(Object.keys(arguments)));
    let keys = '';
    for (const key in arguments) keys += key + ',';
    console.log('for-in', keys);
    try { console.log(arguments.callee); } catch (error) { console.log('callee-read', error instanceof TypeError); }
    try { arguments.callee = inspect; } catch (error) { console.log('callee-write', error instanceof TypeError); }
    return arguments;
}
const saved = (inspect as any)(1, 'two', true);
console.log('returned', saved[0], saved[1], saved[2], saved.length, typeof saved.push, '' + saved);
function capture(value: number): () => IArguments { return () => arguments; }
const first = capture(4), second = capture(5);
console.log('capture', first() === first(), first() !== second(), first()[0], second()[0]);
first()[0] = 8;
console.log('shared', first()[0]);
function defaults(first: number = arguments[1], second = 7): void {
    console.log('defaults', first, second, arguments.length, arguments[0], arguments[1]);
}
defaults(); defaults(undefined, 9);
function rest(first: number, ...tail: number[]): void {
    console.log('rest', first, joined(tail), arguments.length, arguments[2]);
    tail[0] = 99;
    console.log('rest-copy', arguments[1]);
}
rest(1, 2, 3);
function nested(value: number): void {
    const outer = () => arguments;
    function inner(value: number): IArguments { return arguments; }
    console.log('nested', outer()[0], inner(6)[0], outer() === arguments);
}
nested(5);
function iterable(): void {
    const [head, ...tail] = arguments;
    let text = '';
    for (const value of arguments) text += value + ',';
    console.log('iterable', head, joined(tail), text, joined([...arguments]));
    console.log('apply', rest.apply(null, arguments as any));
    arguments.length = 1;
    console.log('shortened', joined([...arguments]), arguments[2], joined(Object.keys(arguments)));
}
(iterable as any)(1, 2, 3);
const receiver = {
    value: 10,
    read(amount: number): number { console.log('receiver', this.value, arguments.length); return arguments[0] + this.value; }
};
console.log('numeric-call', receiver.read(2), receiver.read.call({value: 20}, 3));
function Constructor(this: any, value: number): void {
    this.read = () => arguments;
    console.log('constructor', new.target === Constructor, arguments.length, arguments[0]);
}
const constructed = new (Constructor as any)(7, 8);
console.log('constructed', constructed.read()[1]);
class Base {
    read: () => IArguments;
    constructor(value: number) { this.read = () => arguments; console.log('base', arguments.length, value); }
}
class Derived extends Base {
    own: () => IArguments;
    constructor(value: number, other: number) {
        console.log('before-super', arguments.length, arguments[1], new.target === Derived);
        const own = () => arguments;
        super(arguments[0]);
        this.own = own;
    }
}
const derived = new Derived(11, 12);
console.log('derived', derived.read().length, derived.own().length, derived.own()[1]);
function count(): number { return arguments.length; }
const Count: any = count;
console.log('arity', Count(), Count(undefined), Count(1, 2, 3), Count.length);
const bound = Count.bind(null, 1, 2);
console.log('bound', bound(3), Count.apply(null, [1, 2, 3, 4]));
const recursive = function self(depth: number): number {
    if (depth === 0) return arguments.length;
    return arguments.length + self(depth - 1);
};
console.log('recursive', recursive(3));
const closure = (() => {
    const offset = 10;
    return function(count: number): number { return offset + arguments[0] + arguments.length; };
})();
console.log('closure', closure(4));
const accessors = {
    get value(): number { return arguments.length; },
    set value(next: number) { console.log('setter', arguments.length, arguments[0], next); }
};
console.log('getter', accessors.value);
accessors.value = 8;
function patterns({value}: {value: number}, ...tail: number[]): void {
    console.log('patterns', value, arguments[0].value, arguments.length, tail[0]);
    const copy = {...arguments};
    const {length, ...rest} = arguments;
    console.log('copy', Object.keys(copy).length, Object.hasOwn(copy, 'length'), copy[1], length, rest[2]);
}
patterns({value: 3}, 4, 5);
function dynamicLength(): void {
    arguments[3] = 7;
    console.log('no-growth', arguments.length, arguments[3]);
    arguments.length = 4;
    console.log('grown', joined([...arguments]), Object.keys(arguments).length);
}
(dynamicLength as any)(1);
function defaultMutation(first: number = (arguments[1] = 9), second = 2): void {
    console.log('default-mutation', first, second, arguments[1]);
}
defaultMutation(undefined, 4);
const countAlias = count;
console.log('alias', (countAlias as any)(1, 2));
console.log('iife', (function(): number { return arguments.length; } as any)(1, 2, 3));
function inheritedCallee(): void {
    const child: any = {};
    child.__proto__ = arguments;
    try { child.callee = inspect; } catch (error) { console.log('inherited-callee', error instanceof TypeError); }
    child.length = 99;
    console.log('inherited-length', child.length, arguments.length);
}
inheritedCallee();
class ParameterBase {
    constructor(public value: number) { console.log('parameter-base', arguments.length, this.value); }
}
class ParameterChild extends ParameterBase {
    constructor(public extra: number) { super(extra + 1); console.log('parameter-child', arguments[0], this.extra, this.value); }
}
new ParameterChild(20);
function objectArgument(record: {value: number}): void {
    arguments[0].value = 17;
    console.log('object-alias', record.value);
}
objectArgument({value: 2});
