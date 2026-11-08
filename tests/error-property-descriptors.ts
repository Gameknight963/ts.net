export {};
const error = new Error('original', {cause: 7});
Object.defineProperty(error, 'message', {value: 'changed', enumerable: true, writable: false});
Object.defineProperty(error, 'cause', {get() {return 9;}, enumerable: true});
console.log('values', error.message, error.cause, String(error), Object.keys(error).join(','));
try {error.message = 'wrong';} catch (caught) {console.log('readonly', (caught as Error).name);}
Object.defineProperty(error, 'stack', {value: 'custom', enumerable: true});
console.log('stack', error.stack, Object.keys(error).join(','), Object.getOwnPropertyNames(error).join(','));
console.log('freeze identity', Object.freeze(error) === error);
console.log('frozen', Object.isFrozen(error), Reflect.defineProperty(error, 'message', {value: 'other'}), Reflect.deleteProperty(error, 'stack'));
const assigned = new Error();
assigned.stack = 'assigned';
console.log('assigned stack', assigned.stack, Object.keys(assigned).join(','), Object.getOwnPropertyDescriptor(assigned, 'stack')!.enumerable);
const aggregate = new AggregateError([1, 2], 'many');
Object.defineProperty(aggregate, 'errors', {enumerable: true, writable: false});
console.log('aggregate', aggregate.errors.join(','), Object.keys(aggregate).join(','));
const freeze = Object.freeze;
const readonlyError: Readonly<Error> = freeze(new TypeError('alias'));
console.log('readonly view', readonlyError.message, readonlyError.name, Object.isFrozen(readonlyError));
