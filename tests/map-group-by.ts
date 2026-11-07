const groupBy = (Map as any).groupBy;
const parity = groupBy([1, 2, 3, 4, 5], (value: number, index: number) => {console.log('visit', value, index); return value % 2;});
for (const [key, values] of parity) console.log('group', key, values.join(','));
console.log('function', groupBy.name, groupBy.length, Object.keys(Map).includes('groupBy'));
console.log('receiver', groupBy.call(null, [1], () => 'x') instanceof Map);
const key = {};
const groupingKeys: any[] = [key, key, NaN, NaN, -0, 0];
const identity = groupBy([0, 1, 2, 3, 4, 5], (value: number): any => groupingKeys[value]);
console.log('identity', identity.size, identity.get(key).join(','), identity.get(NaN).join(','), identity.get(0).join(','));
for (const item of identity.keys()) if (item === 0) console.log('zero', 1 / item);
const nullish = groupBy([1, 2, 3], (value: number) => value === 1 ? null : undefined);
console.log('nullish', nullish.get(null).join(','), nullish.get(undefined).join(','));
const letters = groupBy('a\uD83D\uDE00b', (_: string, index: number) => index);
for (const [index, values] of letters) console.log('letter', index, values[0].length);
const list = [1, 2];
const live = groupBy(list, (value: number) => {if (value === 1) list.push(3); return 'all';});
console.log('live', live.get('all').join(','));
const holes = groupBy([, 1, ,], (value: any) => typeof value);
console.log('holes', holes.get('undefined').length, holes.get('number')[0]);
console.log('empty', groupBy([], () => 1).size);
for (const input of [null, undefined, 42, {}]) {
    try {groupBy(input, () => 1);} catch (error) {console.log('invalid input', (error as Error).name);}
}
try {groupBy([], null);} catch (error) {console.log('invalid callback', (error as Error).name);}
class Derived extends Map<any, any> {}
const derived = (Derived as any).groupBy([1], () => 1);
console.log('intrinsic', derived instanceof Map, derived instanceof Derived);
const iterator = new Set([1, 2, 3]).values();
(iterator as any).return = function (): any {console.log('closed', this.next().value); throw 'cleanup error';};
try {groupBy(iterator, () => {throw 'callback error';});} catch (error) {console.log('original', error);}
const originalSet = Map.prototype.set;
Map.prototype.set = function (): any {throw 'set override';};
const bypass = groupBy([1, 2], () => 'all');
Map.prototype.set = originalSet;
console.log('bypass', bypass.get('all').join(','));
