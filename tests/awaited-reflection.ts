export {};

async function identity<T>(value: T): Promise<T> { return value; }
function promised<T>(value: T): Promise<T> { return Promise.resolve(value); }

async function work() {
    const direct = { x: 1, y: 2 };
    const remove = await Promise.resolve(Reflect.deleteProperty);
    console.log(remove(direct, 'x'), Object.hasOwn(direct, 'x'), Object.keys(direct).join(','));

    const stored = { x: 3, y: 4 };
    const pending = Promise.resolve(Reflect.deleteProperty);
    const storedRemove = await pending;
    console.log(storedRemove(stored, 'y'), Object.keys(stored).join(','));

    const returned = { x: 5, y: 6 };
    const returnedRemove = await promised(Reflect.deleteProperty);
    console.log(returnedRemove(returned, 'x'), Object.keys(returned).join(','));

    const asynchronous = { x: 7, y: 8 };
    const asyncRemove = await identity(Reflect.deleteProperty);
    console.log(asyncRemove(asynchronous, 'y'), Object.keys(asynchronous).join(','));

    const conditional = { x: 9, y: 10 };
    const choose = Date.now() >= 0;
    const conditionalRemove = await (choose ? Promise.resolve(Reflect.deleteProperty) : identity(Reflect.deleteProperty));
    console.log(conditionalRemove(conditional, 'x'), Object.keys(conditional).join(','));

    const defined = { x: 11, y: 12 };
    const define = await Promise.resolve(Object.defineProperty);
    define(defined, 'x', { value: 13, writable: false, enumerable: false, configurable: true });
    console.log(defined.x, Object.keys(defined).join(','), Object.getOwnPropertyDescriptor(defined, 'x')!.writable);

    const reflected = { x: 14, y: 15 };
    const reflectDefine = await promised(Reflect.defineProperty);
    console.log(reflectDefine(reflected, 'y', { value: 16, enumerable: false }), reflected.y, Object.keys(reflected).join(','));

    const properties = { x: 17, y: 18 };
    const defineMany = await identity(Object.defineProperties);
    defineMany(properties, { x: { value: 19, enumerable: false }, y: { value: 20, writable: false } });
    console.log(properties.x, properties.y, Object.keys(properties).join(','));

    const namespaced = { x: 21, y: 22 };
    const reflection = await Promise.resolve(Reflect);
    console.log(reflection.deleteProperty(namespaced, 'x'), Object.keys(namespaced).join(','));

    const container = { remove: Promise.resolve(Reflect.deleteProperty) };
    const contained = { x: 23, y: 24 };
    const containedRemove = await container.remove;
    console.log(containedRemove(contained, 'y'), Object.keys(contained).join(','));

    const untouched = { x: 25, y: 26 };
    const unrelated = { resolve(value: number) { return value + 1; } };
    console.log(await unrelated.resolve(untouched.x), untouched.y);
}

work();
