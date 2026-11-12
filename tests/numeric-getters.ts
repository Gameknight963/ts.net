class Parent {
    base = 3;
    get value(): number {return this.base + 1;}
    static base = 5;
    static get value(): number {return this.base + 1;}
}
class Child extends Parent {
    override get value(): number {return super.value + 2;}
    static override get value(): number {return super.value + 2;}
}
const child = new Child();
console.log('class', child.value, Child.value);
const get = Object.getOwnPropertyDescriptor(Parent.prototype, 'value')!.get!;
console.log('borrow', get.call({base: 11}), Reflect.get(Parent.prototype, 'value', {base: 13}));
class Missing {get value(): number | undefined {return undefined;}}
class Special {get zero(): number {return -0;} get nan(): number {return NaN;}}
console.log('special', new Missing().value === undefined, Object.is(new Special().zero, -0), Number.isNaN(new Special().nan));
interface Value {readonly value: number;}
function read(value: Value): number {return value.value;}
const described: any = Object.create(null);
described.base = 17;
Object.defineProperty(described, 'value', {get(this: {base: number}, first = 19): number {
    console.log('arguments', arguments.length, arguments[0] === undefined);
    return this.base + first;
}, configurable: true});
console.log('descriptor', read(described));
function make(this: {base: number}): Value {
    const value: any = Object.create(null);
    Object.defineProperty(value, 'value', {get: () => this.base});
    return value;
}
const lexical = make.call({base: 23});
console.log('lexical', lexical.value, Reflect.get(lexical, 'value', {base: 29}));
const inherited: any = Object.create({get value(): number {console.log('inherited receiver', this === inherited); return 31;}});
console.log('prototype', read(inherited));
const object = {base: 37, get value(): number {return this.base + 1;}};
console.log('object', object.value, read(object));
class Throwing {get value(): number {throw new Error('getter');}}
try {console.log(new Throwing().value);} catch (error) {console.log('error', (error as Error).message);}
Object.defineProperty(child, 'value', {get() {return 41;}, configurable: true});
console.log('redefined', child.value);
delete (child as any).value;
console.log('deleted', child.value);
const setterOnly: any = Object.create(null);
Object.defineProperty(setterOnly, 'value', {set(value: number) {console.log('set only', value);}});
console.log('setter only', read(setterOnly));
setterOnly.value = 43;
