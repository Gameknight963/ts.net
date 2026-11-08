export {};
class ChildMap extends Map {}
class ChildSet extends Set {}
console.log('identity', Map[Symbol.species] === Map, Set[Symbol.species] === Set, ChildMap[Symbol.species] === ChildMap, ChildSet[Symbol.species] === ChildSet);
for (const constructor of [Map, Set]) {
    const descriptor = Object.getOwnPropertyDescriptor(constructor, Symbol.species)!;
    console.log('descriptor', descriptor.get!.name, descriptor.get!.length, descriptor.set, descriptor.enumerable, descriptor.configurable);
    for (const receiver of [null, undefined, 42, {}, constructor]) console.log('receiver', descriptor.get!.call(receiver) === receiver);
    try {(constructor as any)[Symbol.species] = null;} catch (error) {console.log('readonly', (error as Error).name);}
}
let reads = 0;
class CustomSet extends Set<number> {
    static get [Symbol.species](): SetConstructor {reads++; throw new Error('unused species');}
}
const union = (new CustomSet([1]) as any).union(new Set([2]));
console.log('union', union instanceof Set, union instanceof CustomSet, [...union].join(','), reads);
