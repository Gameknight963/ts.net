export {};
for (const key of ['message', 'cause']) {
    const error = new Error();
    error.stack = 'fixed stack';
    let reads = 0;
    Object.defineProperty(error, key, {get() {reads++; throw new Error('getter must not run');}, configurable: true});
    try {const copy: any = structuredClone(error); console.log('accessor', key, reads, Object.hasOwn(copy, key), copy[key], copy.stack);}
    catch (error) {console.log('failure', key, reads, (error as Error).message);}
}
for (const value of ['text', 42, undefined, null]) {
    const error = new Error(); error.stack = 'fixed stack';
    Object.defineProperty(error, 'message', {value, configurable: true});
    const copy = structuredClone(error);
    console.log('data', typeof copy.message, copy.message, Object.hasOwn(copy, 'message'));
}
const error = new Error('outer'); error.stack = 'fixed stack';
Object.defineProperty(error, 'cause', {value: {owner: error}});
const copy = structuredClone(error);
console.log('cycle', (copy.cause as any).owner === copy, copy.message);
for (const value of [new WeakRef({}), new FinalizationRegistry(() => {})]) {
    try {structuredClone(value); console.log('unexpected');}
    catch (error) {console.log('weak object', (error as DOMException).name, (error as DOMException).code);}
}
