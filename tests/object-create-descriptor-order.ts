export {};
const symbol = Symbol('later');
let effects = '';
const descriptors: any = Object.create(null, {
    a: {enumerable: true, get() {
        effects += 'A';
        delete descriptors.b; descriptors.b = {value: 2, enumerable: true};
        delete descriptors[symbol]; descriptors[symbol] = {value: 3, enumerable: true};
        descriptors.added = {value: 4, enumerable: true};
        return {value: 1, enumerable: true};
    }},
    b: {value: {}, enumerable: false, configurable: true},
    [symbol]: {value: {}, enumerable: false, configurable: true},
    hidden: {get() {throw new Error('hidden descriptor');}}
});
const result = Object.create(null, descriptors);
console.log('created', JSON.stringify(result), result[symbol], Object.hasOwn(result, 'added'), effects);
let reads = '';
const invalid: any = {
    get first() {reads += 'F'; return {value: 1};},
    get second() {reads += 'S'; return {get value() {reads += 'V'; return 2;}, get get() {reads += 'G'; return () => 3;}};},
    get third() {reads += 'T'; return {value: 4};}
};
try {Object.create(null, invalid);} catch (error) {console.log('invalid', (error as Error).name, reads);}
