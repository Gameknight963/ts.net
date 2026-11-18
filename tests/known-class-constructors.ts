export {};
const events: string[] = [];
function initial(): number {events.push('field'); return 1;}
function argument(value: number): number {events.push(`argument:${value}`); return value;}
class Item {
    static initialized = events.push('static');
    value = initial();
    constructor(first: number, second: number) {events.push('constructor'); this.value += first + second;}
    read(): number {return this.value;}
}
const first = new Item(argument(2), argument(3));
const second = new Item(argument(4), argument(5));
console.log('order', events.join(','), first.read(), second.read(),
    Object.getPrototypeOf(first) === Item.prototype, first instanceof Item);
events.length = 0;
let published: any;
function publish(value: any): number {published = value; events.push(`keys:${Object.keys(value).join(',')}`); return 2;}
class Escaped {
    first = publish(this);
    second = initial();
    constructor(value: number) {events.push(`body:${Object.keys(this).join(',')}`); this.first += value;}
}
const escaped = new Escaped(7);
console.log('escaped', escaped === published, escaped.first, escaped.second, events.join(','));
function freeze(value: any): number {published = value; Object.freeze(value); return 3;}
class Frozen {
    value = freeze(this);
    constructor(value: number) {events.push('unexpected body'); this.value += value;}
}
try {new Frozen(1);} catch (error) {console.log('frozen', (error as Error).name, Object.keys(published).length);}
class Replaced {
    value = initial();
    constructor(value: number) {events.push('replace'); return {value: value + 10};}
}
const replaced = new Replaced(8);
console.log('replacement', replaced.value, replaced instanceof Replaced);
class Primitive {
    value = initial();
    constructor(value: number) {events.push(`primitive:${value}`); return 3 as any;}
}
console.log('primitive', new Primitive(9).value);
class EmptyArguments {
    value = initial();
    constructor() {events.push('zero constructor');}
}
console.log('zero', new EmptyArguments().value);
function capture(offset: number): number {
    class Captured {value = offset; constructor(value: number) {this.value += value;}}
    return new Captured(4).value;
}
console.log('captured fallback', capture(1), capture(2));
function make(label: string) {
    class Named {value = initial(); read(): string {return label + ':' + this.value;}}
    return new Named();
}
const namedA = make('a'), namedB = make('b');
console.log('distinct owners', namedA.read(), namedB.read(), Object.getPrototypeOf(namedA) !== Object.getPrototypeOf(namedB));
class Defaults {value: number; constructor(value = initial()) {this.value = value;}}
console.log('default fallback', new Defaults().value, new Defaults(12).value);
class Observed {value = 0; constructor(value: number) {this.value = value + arguments.length;}}
console.log('arguments fallback', new Observed(13).value);
class ReturnedArguments {value = 0; constructor(value: number) {return {value: value + arguments.length};}}
console.log('returned arguments fallback', new ReturnedArguments(14).value);
class Parameterized {constructor(public value: number) {events.push('parameter property');}}
console.log('parameter property', new Parameterized(16).value);
function target(value: any): number {events.push(`target:${value.name}`); return 14;}
class Target {value = 0; constructor() {this.value = target(new.target);}}
console.log('target fallback', new Target().value);
class Derived extends Item {extra = initial(); constructor(value: number) {super(value, 1);}}
console.log('derived fallback', new Derived(15).read());
function early(): Late {return new Late(argument(99));}
try {early();} catch (error) {console.log('early binding', (error as Error).name);}
class Late {value = 0; constructor(value: number) {this.value = initial() + value;}}
console.log('late binding', new Late(1).value);
console.log('effects', events.join(','));
