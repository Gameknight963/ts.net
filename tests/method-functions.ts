export {};
function create(prefix: string) {
    return {
        value: 2,
        write(delta: number): void {this.value += delta;},
        text(value: string): string {return prefix + value + this.value;},
        read(): number {return this.value + 1;},
        pair(first: number, second: number): any {return this.value + first + second;},
        get amount(): number {let value = this.value; return value + 1;},
        get dynamic(): any {return this.value + 2;},
        defaults(value = 'default'): string {return value;},
        rest(...values: number[]): number {return values.length;},
        observed(value: number): number {return value + arguments.length;},
        arrow() {return (delta: number): number => this.value += delta;}
    };
}
const value = create('prefix:');
value.write(3);
console.log('methods', value.value, value.text('text:'), value.read(), value.pair(1, 2));
const numeric: {pair(first: number, second: number): number; amount: number; dynamic: number} = value;
console.log('numeric views', numeric.pair(2, 3), numeric.amount, numeric.dynamic);
console.log('fallbacks', value.defaults(), value.rest(1, 2, 3), value.observed(4));
const borrowed: any = {value: 10};
console.log('borrowed', value.text.call(borrowed, 'other:'), Reflect.apply(value.read, borrowed, []));
const bound = value.text.bind(borrowed, 'bound:');
console.log('bound', bound());
const detached = value.read;
try {detached();} catch {console.log('detached receiver');}
const arrow = value.arrow();
console.log('arrow', arrow.call(borrowed, 2), value.value, borrowed.value);
const descriptors = Object.getOwnPropertyDescriptors(value);
console.log('getter receiver', Reflect.apply(descriptors.amount!.get!, borrowed, []));
const proxy = new Proxy(value, {get(target, key, receiver) {
    console.log('proxy get', String(key)); return Reflect.get(target, key, receiver);
}});
console.log('proxy method', proxy.read(), proxy.amount);
const first = create('first:'), second = create('second:');
console.log('identities', first.write === second.write, first.text('x'), second.text('x'));
console.log('source', value.write.toString().includes('this.value'));

const visitor = {total: 0, visit(value: number, index: number): void {this.total += value + index;}};
[1, 2, 3].forEach(visitor.visit, visitor);
console.log('callback receiver', visitor.total);
const constructors = {Ctor: function(this: any, value: number): void {this.value = value; this.target = new.target;}};
const constructed: any = new (constructors.Ctor as any)(9);
console.log('function property constructor', constructed.value, constructed.target === constructors.Ctor);
const called: any = {};
constructors.Ctor.call(called, 4);
console.log('function property call', called.value, called.target);
