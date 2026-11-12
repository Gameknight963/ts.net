export {};
const asyncFunction = async function work() {return 1;};
const generator = function* values() {yield 1;};
const asyncGenerator = async function* valuesAsync() {yield 1;};
for (const value of [asyncFunction, generator, asyncGenerator]) {
    const prototype = Object.getPrototypeOf(value);
    const constructor = prototype.constructor;
    console.log('family', constructor.name, constructor.length, value.constructor === constructor, typeof prototype, Object.prototype.toString.call(value), Object.prototype.toString.call(prototype));
    console.log('inheritance', Object.getPrototypeOf(prototype) === Function.prototype, Object.getPrototypeOf(constructor) === Function, value instanceof constructor, value instanceof Function);
    const own = Object.getOwnPropertyDescriptor(constructor, 'prototype')!;
    const back = Object.getOwnPropertyDescriptor(prototype, 'constructor')!;
    console.log('descriptors', own.value === prototype, own.writable, own.enumerable, own.configurable, back.value === constructor, back.writable, back.enumerable, back.configurable);
    console.log('bound', value.bind(null) instanceof constructor, Object.getPrototypeOf(value.bind(null)) === prototype);
}
for (const value of [generator, asyncGenerator]) {
    const functionPrototype = Object.getPrototypeOf(value);
    const iteratorPrototype = functionPrototype.prototype;
    const instance = value();
    console.log('generator prototypes', Object.getPrototypeOf(value.prototype) === iteratorPrototype, Object.getPrototypeOf(Object.getPrototypeOf(instance)) === iteratorPrototype, iteratorPrototype.constructor === functionPrototype, instance.constructor === functionPrototype);
    const descriptor = Object.getOwnPropertyDescriptor(functionPrototype, 'prototype')!;
    console.log('iterator descriptor', descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
console.log('async prototype slot', Object.hasOwn(Object.getPrototypeOf(asyncFunction), 'prototype'), Object.hasOwn(asyncFunction, 'prototype'));

const anotherAsync = async () => 2;
function* anotherGenerator() {yield 2;}
async function* anotherAsyncGenerator() {yield 2;}
console.log('shared prototypes', Object.getPrototypeOf(asyncFunction) === Object.getPrototypeOf(anotherAsync), Object.getPrototypeOf(generator) === Object.getPrototypeOf(anotherGenerator), Object.getPrototypeOf(asyncGenerator) === Object.getPrototypeOf(anotherAsyncGenerator));
console.log('different constructors', asyncFunction.constructor !== generator.constructor, generator.constructor !== asyncGenerator.constructor, asyncFunction.constructor !== Function);
for (const value of [asyncFunction, generator, asyncGenerator]) {
    const original = Object.getPrototypeOf(value);
    const custom = Object.create(original);
    custom.marker = 3;
    Object.setPrototypeOf(value, custom);
    console.log('custom prototype', (value as any).marker, value.constructor === original.constructor, value instanceof original.constructor);
    Object.setPrototypeOf(value, original);
    console.log('restored prototype', Object.getPrototypeOf(value) === original);
    try {Reflect.apply(original, null, []);} catch (error) {console.log('prototype not callable', (error as Error).name);}
}

{
const families: any[] = [Object.getPrototypeOf(async function(){}).constructor, Object.getPrototypeOf(function*(){}).constructor, Object.getPrototypeOf(async function*(){}).constructor];
for (const Constructor of families) {
    const fn = new Constructor();
    console.log('empty family', Constructor.name, fn.name, fn.length, fn instanceof Constructor, Object.getPrototypeOf(fn) === Constructor.prototype);
    console.log('empty source', JSON.stringify(fn.toString()));
    console.log('empty keys', Object.getOwnPropertyNames(fn).join(','));
    const called = Constructor();
    console.log('family call', called.name, called !== fn);
    try {new fn();} catch(error) {console.log('not constructible', (error as Error).name);}
    class Derived extends Constructor {marker = 7;}
    const derived: any = new Derived();
    console.log('derived family', typeof derived, derived.marker, derived instanceof Derived, derived instanceof Constructor);
    function Alternate() {}
    const alternate: any = Reflect.construct(Constructor, [], Alternate);
    console.log('family new target', typeof alternate, Object.getPrototypeOf(alternate) === Alternate.prototype, alternate.name);
    const fallbackTarget: any = function() {};
    fallbackTarget.prototype = 1;
    const fallback = Reflect.construct(Constructor, [], fallbackTarget);
    console.log('family prototype fallback', Object.getPrototypeOf(fallback) === Constructor.prototype);

    if(Constructor.name === 'GeneratorFunction') {
        const iterator = fn(); console.log('empty next', JSON.stringify(iterator.next()), JSON.stringify(iterator.next()));
        console.log('empty iterator prototype', Object.getPrototypeOf(iterator) === fn.prototype);
    } else if(Constructor.name === 'AsyncFunction') {
        fn().then((value: unknown) => console.log('empty async result', value));
    } else {
        fn().next().then((value: unknown) => console.log('empty async next', JSON.stringify(value)));
    }
}
}
