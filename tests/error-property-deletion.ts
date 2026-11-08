export {};
const error: any = new Error('message', { cause: 42 });
console.log('initial', Object.hasOwn(error, 'stack'), typeof error.stack, Object.keys(error).join(','));
for (const key of ['stack', 'message', 'cause']) {
    console.log('delete', key, Reflect.deleteProperty(error, key), Object.hasOwn(error, key), key in error, error[key]);
    error[key] = 'recreated';
    const descriptor = Object.getOwnPropertyDescriptor(error, key)!;
    console.log('recreated', key, error[key], descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
console.log('keys', Object.keys(error).sort().join(','));
let keys: string[] = [];
for (const key in error) keys.push(key);
console.log('for-in', keys.sort().join(','));
delete error.stack;
delete error.message;
delete error.cause;
console.log('empty', Object.getOwnPropertyNames(error).length, Object.keys(error).length);
Object.setPrototypeOf(error, { stack: 'inherited' });
console.log('inherited', error.stack, Object.hasOwn(error, 'stack'));
const assigned: any = new Error('assigned');
assigned.stack = 'replacement';
console.log('assigned', Object.getOwnPropertyDescriptor(assigned, 'stack')!.enumerable);
delete assigned.stack;
console.log('assigned deleted', assigned.stack, Object.hasOwn(assigned, 'stack'));
const aggregate: any = new AggregateError([1, 2], 'aggregate');
delete aggregate.errors;
aggregate.errors = [3];
console.log('errors recreated', Object.keys(aggregate).join(','), aggregate.errors.join(','));
