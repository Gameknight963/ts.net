export {};
class StackValue { base = 1; get value(): number { return this.base; } }
function tdz(): number {
    let total = 0;
    for (let index = 0; index < 64; index++)
        total += delayed.value;
    return total;
}
function range() {
    return new Array(-1);
}
function type() {
    return (null as any).missing;
}
function inspect(label: string, action: () => unknown, line: number) {
    try { action(); } catch (error) {
        const value = error as Error;
        const stack = (value.stack ?? '').replace(/:line (\d+)/g, ':$1');
        console.log(label, value.name, stack.includes('stack-location-native-errors.ts:' + line));
        const descriptor = Object.getOwnPropertyDescriptor(value, 'stack')!;
        console.log('descriptor', descriptor.enumerable, descriptor.configurable, Object.keys(value).length);
        value.stack = 'changed';
        console.log('changed', value.stack);
        delete (value as any).stack;
        console.log('deleted', Object.hasOwn(value, 'stack'), typeof value.stack);
    }
}
inspect('tdz', tdz, 6);
const delayed = new StackValue();
inspect('range', range, 10);
inspect('type', type, 13);
const original = new Error('original');
try { throw original; } catch (error) {
    console.log('identity', error === original, ((error as Error).stack ?? '').replace(/:line (\d+)/g, ':$1').includes('stack-location-native-errors.ts:32'));
}
try { throw 123; } catch (error) { console.log('primitive', error === 123); }
async function asynchronous() {
    await 0;
    return new Array(-1);
}
asynchronous().catch(error => console.log('async', error.name, (error.stack ?? '').replace(/:line (\d+)/g, ':$1').includes('stack-location-native-errors.ts:39')));
