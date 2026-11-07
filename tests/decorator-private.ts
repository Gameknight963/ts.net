const readers: ((receiver: any) => any)[] = [];
function field(_value: undefined, context: ClassFieldDecoratorContext<any, number>) {
    console.log('field', context.name, context.static, context.private);
    readers.push(context.access.get);
    context.addInitializer(function(this: any) { console.log('field-extra', context.name, context.access.has(this), context.access.get(this)); });
    return (value: number) => value + 1;
}
function method(value: any, context: ClassMethodDecoratorContext<any, any>) {
    console.log('method', context.name, context.static, context.private);
    context.addInitializer(function(this: any) { console.log('method-extra', context.name, context.access.has(this)); });
    return function(this: any, ...args: any[]) { return value.apply(this, args) + 2; };
}
function getter(value: (this: any) => number, context: ClassGetterDecoratorContext<any, number>) {
    console.log('getter', context.name, context.private);
    return function(this: any) { return value.call(this) + 3; };
}
function setter(value: (this: any, value: number) => void, context: ClassSetterDecoratorContext<any, number>) {
    console.log('setter', context.name, context.private);
    return function(this: any, input: number) { value.call(this, input * 2); };
}
function accessor(value: ClassAccessorDecoratorTarget<any, number>, context: ClassAccessorDecoratorContext<any, number>) {
    console.log('accessor', context.name, context.static, context.private);
    return {get(this: any) { return value.get.call(this) + 4; }, set(this: any, input: number) { value.set.call(this, input * 2); }, init(input: number) { return input + 5; }};
}
class Example {
    @field #value = 1;
    @field static #staticValue = 10;
    @method #method() { return this.#value; }
    @method static #staticMethod() { return this.#staticValue; }
    @getter get #read() { return this.#value; }
    @setter set #read(value: number) { this.#value = value; }
    @accessor accessor #number = 3;
    @accessor static accessor #staticNumber = 20;
    read() { return [this.#value, this.#method(), this.#read, this.#number].join(','); }
    write() { this.#read = 7; this.#number = 8; }
    static read() { return [this.#staticValue, this.#staticMethod(), this.#staticNumber].join(','); }
}
const value = new Example();
console.log(value.read(), Example.read());
value.write(); console.log(value.read());
console.log(readers[0](Example), readers[1](value));
try { readers[0]({}); } catch (error) { console.log(error instanceof TypeError); }
function replace(value: any, context: ClassDecoratorContext): any {
    console.log(context.kind, context.name);
    context.addInitializer(function(this: any) { console.log('class-extra', this.read()); });
    return class extends value {};
}
@replace
class Replaced {
    @field static #value = 30;
    static read() { return this.#value; }
}
console.log(Replaced.read());
