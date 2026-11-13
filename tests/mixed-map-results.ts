const mixedMapSource: (number | string)[] = [];
for (let index = 0; index < 10000; index++) mixedMapSource.push(index & 1 ? 'text' : index);
let inheritedMapReads = 0;
let mixedMapResult: string[] = [];
try {
    mixedMapResult = mixedMapSource.map((value, index, source) => {
        if (index === 0) {
            source.length = 17;
            delete source[1];
            Object.defineProperty(Array.prototype, '1', {get() { inheritedMapReads++; return 'inherited'; }, configurable: true});
        }
        return String(value);
    });
} finally { Reflect.deleteProperty(Array.prototype, '1'); }
console.log('mixed', mixedMapResult.length, Object.keys(mixedMapResult).length, inheritedMapReads);
console.log('values', mixedMapResult[0], mixedMapResult[1], mixedMapResult[16], mixedMapResult[17]);
const mixedDescriptor = Object.getOwnPropertyDescriptor(mixedMapResult, '1')!;
console.log('descriptor', mixedDescriptor.writable, mixedDescriptor.enumerable, mixedDescriptor.configurable);
const sparseMixed: (number | string)[] = new Array(10000);
sparseMixed[9999] = 'last';
const sparseMapped = sparseMixed.map(value => String(value));
console.log('sparse', sparseMapped.length, Object.keys(sparseMapped).join(','), sparseMapped[9999], Object.hasOwn(sparseMapped, 0));
let escapedMixed: any;
function MixedSpecies(length: number): any { escapedMixed = new Array(length); return escapedMixed; }
const customMixed: (number | string)[] = [1, 'text', 3];
Object.defineProperty(customMixed, 'constructor', {value: {[Symbol.species]: MixedSpecies}});
try { customMixed.map(value => { Object.freeze(escapedMixed); return String(value); }); }
catch (error) { console.log('custom', (error as Error).name, Object.isFrozen(escapedMixed), Object.keys(escapedMixed).length); }
