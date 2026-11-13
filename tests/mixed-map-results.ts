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

const numericMixedInput: (number | string | undefined)[] = [1, 'abc', undefined, -0, NaN];
const numericMixedOutput = numericMixedInput.map(value => typeof value === 'string' ? value.length : value);
console.log('nullable-numeric', numericMixedOutput.join(','), Object.hasOwn(numericMixedOutput, 2), numericMixedOutput[2] === undefined, Object.is(numericMixedOutput[3], -0), Number.isNaN(numericMixedOutput[4]));
console.log('numeric-predicates', numericMixedInput.filter(value => typeof value === 'number' ? value : undefined).join(','), numericMixedInput.every(() => 1), numericMixedInput.some(() => undefined), numericMixedInput.findIndex(value => typeof value === 'string' ? value.length : 0));
let numericMixedVisits = 0;
const mutableNumericMixed: (number | string)[] = [1, 'abc', 3];
const mutableNumericOutput = mutableNumericMixed.map(value => { if (++numericMixedVisits === 1) { delete mutableNumericMixed[1]; mutableNumericMixed[2] = 'changed'; } return typeof value === 'number' ? value : value.length; });
console.log('numeric-mutation', mutableNumericOutput.join(','), Object.keys(mutableNumericOutput).join(','), numericMixedVisits);
const customNumericMixed: (number | string)[] = [1, 'abc'];
Object.defineProperty(customNumericMixed, 'constructor', {value: {[Symbol.species]: MixedSpecies}});
const customNumericOutput = customNumericMixed.map(value => typeof value === 'number' ? value : value.length);
console.log('numeric-species', customNumericOutput === escapedMixed, customNumericOutput.join(','));
