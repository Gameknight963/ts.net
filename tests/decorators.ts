function wrap(value: any, context: ClassMethodDecoratorContext<any, any>) {
    const access = context.access;
    context.addInitializer(function(this: any) { console.log('method-init', String(context.name), access.has(this), access.get(this) === this[context.name]); });
    return function(this: any, ...args: any[]) { console.log('wrapped', String(context.name)); return value.apply(this, args) + 1; };
}
function field(label: string) {
    console.log('evaluate', label);
    return function(_value: undefined, context: ClassFieldDecoratorContext<any, number>) {
        console.log('decorate', label, context.kind, String(context.name), context.static, context.private);
        context.addInitializer(function(this: any) { console.log('field-init', label, context.access.has(this), context.access.get(this)); });
        return function(this: any, value: number) { console.log('transform', label, value); return value + 1; };
    };
}
function getter(value: (this: any) => number, context: ClassGetterDecoratorContext<any, number>) {
    console.log(context.kind, context.name);
    return function(this: any) { return value.call(this) + 10; };
}
function setter(value: (this: any, value: number) => void, context: ClassSetterDecoratorContext<any, number>) {
    console.log(context.kind, context.name);
    return function(this: any, input: number) { value.call(this, input * 2); };
}
function accessor(value: ClassAccessorDecoratorTarget<any, number>, context: ClassAccessorDecoratorContext<any, number>): ClassAccessorDecoratorResult<any, number> {
    console.log(context.kind, context.name);
    return {get(this: any) { return value.get.call(this) + 1; }, set(this: any, input: number) { value.set.call(this, input * 2); }, init(input: number) { return input + 3; }};
}
class Example {
    @field('outer') @field('inner') value = 3;
    @wrap method(input: number) { return this.value + input; }
    @getter get read() { return this.value; }
    @setter set read(input: number) { this.value = input; }
    @accessor accessor number = 5;
}
const first = new Example();
console.log(first.value, first.method(7), first.read, first.number);
first.read = 10;
first.number = 10;
console.log(first.value, first.number);
const second = new Example();
console.log(second.value, second.number);
const methodKey = Symbol('method');
class Computed { @wrap [methodKey]() { return 6; } }
console.log(new Computed()[methodKey]());

function replacement<T extends new (...args: any[]) => {}>(value: T, context: ClassDecoratorContext) {
    console.log('class-decorator', context.name);
    context.addInitializer(function(this: any) { console.log('class-ready', this.name); });
    return class extends value { decorated = true; };
}
@replacement
class Replaced { value = 7; constructor(public input: number) {} read() { return this.value + this.input; } }
const replaced = new Replaced(8);
console.log(replaced.value, replaced.read(), replaced instanceof Replaced, (replaced as any).decorated);

class Base { method() { return 1; } }
@replacement
class Derived extends Base {
    @wrap method() { return super.method() + 2; }
    ctor() { return Derived; }
    static self = Derived;
}
const derived = new Derived();
console.log(derived.method(), derived.ctor() === Derived, Derived.self === Derived);

function factory(start: number) {
    @replacement
    class Product { @field('factory') value = start; }
    return Product;
}
const First = factory(1), Second = factory(2);
console.log(new First().value, new Second().value, new First() instanceof Second);

function bound(_value: any, context: ClassMethodDecoratorContext<any, any>) {
    context.addInitializer(function(this: any) { this[context.name] = this[context.name].bind(this); });
}
class Bound { value = 42; @bound read() { return this.value; } }
const extracted = new Bound().read;
console.log(extracted());
