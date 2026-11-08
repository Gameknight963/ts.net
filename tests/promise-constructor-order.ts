export {};
for (const invalid of [false, true]) for (const throwing of [false, true]) {
    let log = '';
    const prototype = {};
    function Alternate() {}
    const target = Alternate.bind(null);
    Object.defineProperty(target, 'prototype', {get() {log += 'P'; if (throwing) throw new Error('prototype'); return prototype;}});
    const executor = function(this: unknown, resolve: (value: number) => void, reject: (reason: unknown) => void) {log += 'E'; console.log('executor', this === undefined, arguments.length); resolve(7);};
    try {
        const value = Reflect.construct(Promise, [invalid ? null : executor], target);
        console.log('constructed', invalid, throwing, Object.getPrototypeOf(value) === prototype, log);
        Promise.prototype.then.call(value, result => console.log('resolved', result));
    } catch (error) {console.log('error', invalid, throwing, (error as Error).name, log);}
}
class Derived extends Promise<number> {
    field = 8;
    #secret = 9;
    read() {return this.#secret;}
}
const derived = new Derived(resolve => resolve(10));
console.log('subclass', derived.field, derived.read(), derived instanceof Promise, derived instanceof Derived);
derived.then(value => console.log('subclass resolved', value));
