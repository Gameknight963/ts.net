export {};
for (const initiallyEnumerable of [false, true]) {
    const source: any = Object.create(null);
    Object.defineProperty(source, 'first', {enumerable: true, get() {Object.defineProperty(source, 'later', {enumerable: !initiallyEnumerable}); return 1;}});
    Object.defineProperty(source, 'later', {value: 2, enumerable: initiallyEnumerable, configurable: true});
    const copy = structuredClone(source);
    console.log('flags', initiallyEnumerable, Object.keys(copy).join(','), copy.first, copy.later);
}
const source: any = Object.create(null);
let reads = 0;
Object.defineProperty(source, 'first', {enumerable: true, get() {Object.defineProperty(source, 'later', {enumerable: false}); source.added = 3; return 1;}});
Object.defineProperty(source, 'later', {enumerable: true, configurable: true, get() {reads++; throw new Error('hidden getter');}});
try {const copy = structuredClone(source); console.log('hidden', Object.keys(copy).join(','), reads, Object.hasOwn(copy, 'added'));}
catch (error) {console.log('failure', reads, (error as Error).message);}
const array = [1, 2];
Object.defineProperty(array, '0', {enumerable: true, get() {Object.defineProperty(array, '1', {enumerable: false}); return 7;}});
const copied = structuredClone(array);
console.log('array', copied.length, copied[0], Object.hasOwn(copied, '1'), Object.keys(copied).join(','));
const deleted: any = Object.create(null);
Object.defineProperty(deleted, 'first', {enumerable: true, get() {delete deleted.later; deleted.added = 3; return 1;}});
deleted.later = 2;
const deletedCopy = structuredClone(deleted);
console.log('deleted and added', Object.keys(deletedCopy).join(','), Object.hasOwn(deletedCopy, 'later'), Object.hasOwn(deletedCopy, 'added'));
