export {};
for (const freeze of [false, true]) {
    let stored = 2;
    const value: any = {field: 1, nested: {field: 3}};
    Object.defineProperty(value, 'accessor', {get() {return stored;}, set(next: number) {stored = next;}, enumerable: true, configurable: true});
    const operation = freeze ? Object.freeze : Object.seal;
    console.log('identity', operation(value) === value);
    console.log('integrity', freeze, Object.isExtensible(value), Object.isSealed(value), Object.isFrozen(value));
    try {value.field = 4;} catch (error) {console.log('write', (error as Error).name);}
    value.accessor = 5;
    value.nested.field = 6;
    console.log('values', value.field, value.accessor, value.nested.field);
    console.log('delete', Reflect.deleteProperty(value, 'field'));
    console.log('define', Reflect.defineProperty(value, 'field', {value: 7}), Reflect.defineProperty(value, 'added', {value: 8}));
    console.log('descriptor', JSON.stringify(Object.getOwnPropertyDescriptor(value, 'field')));
    console.log('repeat', operation(value) === value);
}
const accessor = Object.create(null, {field: {get() {return 1;}, configurable: true}});
Object.freeze(accessor);
console.log('accessor frozen', Object.isFrozen(accessor));
for (const value of [null, undefined, 1, 'text', Symbol('value')]) console.log('primitive', Object.freeze(value) === value, Object.seal(value) === value);
