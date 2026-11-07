const groupBy = (Object as any).groupBy;
const parity = groupBy([1, 2, 3, 4], (value: number, index: number) => {console.log('visit', value, index); return value % 2;});
for (const key of Object.keys(parity)) console.log('group', key, parity[key].join(','));
console.log('prototype', Object.getPrototypeOf(parity) === null);
console.log('function', groupBy.name, groupBy.length, Object.keys(Object).includes('groupBy'));
console.log('receiver', Object.getPrototypeOf(groupBy.call(null, [1], () => 'x')) === null);
const dangerous = groupBy(['__proto__', 'constructor', 'toString', '__proto__'], (value: string) => value);
console.log('special', Object.getPrototypeOf(dangerous) === null, dangerous.__proto__.length, dangerous.constructor[0], dangerous.toString[0]);
const keys: any[] = [0, -0, true, null, undefined, NaN];
const primitives = groupBy(keys, (value: any) => value);
for (const key of Object.keys(primitives)) console.log('primitive', key, primitives[key].length);
const objectKey = {toString(): string {console.log('coerce'); return 'same';}};
const objects = groupBy([1, 2], () => objectKey);
console.log('objects', objects.same.join(','));
const fallbackKey = {toString(): any {return {};}, valueOf(): number {return 17;}};
console.log('fallback', groupBy([3], () => fallbackKey)['17'][0]);
const strings = groupBy('a\uD83D\uDE00b', (_: string, index: number) => index);
for (const key of Object.keys(strings)) console.log('string', key, strings[key][0].length);
const live = [1, 2];
console.log('live', groupBy(live, (value: number) => {if (value === 1) live.push(3); return 'all';}).all.join(','));
console.log('empty', Object.keys(groupBy([], () => 'none')).length);
for (const input of [null, undefined, 42, {}]) {
    try {groupBy(input, () => 'a');} catch (error) {console.log('invalid input', (error as Error).name);}
}
try {groupBy([], null);} catch (error) {console.log('invalid callback', (error as Error).name);}
const iterator = new Set([1, 2, 3]).values();
(iterator as any).return = function (): any {console.log('closed', this.next().value); throw 'cleanup error';};
try {groupBy(iterator, () => ({toString(): never {throw 'key error';}}));} catch (error) {console.log('original', error);}
const bad = {toString(): any {return {};}, valueOf(): any {return {};}};
try {groupBy([1], () => bad);} catch (error) {console.log('bad key', (error as Error).name);}
