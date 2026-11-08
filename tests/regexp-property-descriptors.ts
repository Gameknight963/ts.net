export {};
const regex = /a/g;
Object.defineProperty(regex, 'lastIndex', {value: 1, writable: false});
console.log('descriptor', JSON.stringify(Object.getOwnPropertyDescriptor(regex, 'lastIndex')));
console.log('same value', Reflect.defineProperty(regex, 'lastIndex', {value: 1}), Reflect.defineProperty(regex, 'lastIndex', {value: 0}));
console.log('flags', Reflect.defineProperty(regex, 'lastIndex', {enumerable: true}), Reflect.defineProperty(regex, 'lastIndex', {configurable: true}), Reflect.deleteProperty(regex, 'lastIndex'));
try {regex.lastIndex = 2;} catch (error) {console.log('assignment', (error as Error).name);}
try {regex.test('ba');} catch (error) {console.log('global update', (error as Error).name, regex.lastIndex);}
const ordinary = Object.freeze(/a/);
console.log('nonglobal', ordinary.test('a'), ordinary.exec('ba')![0], Object.isFrozen(ordinary));
const failed = Object.freeze(/z/g);
try {failed.exec('a');} catch (error) {console.log('failed update', (error as Error).name);}
const extra = /a/;
Object.defineProperty(extra, 'label', {get() {return this.source;}, enumerable: true});
Object.defineProperty(extra, 'hidden', {value: 7});
console.log('properties', (extra as any).label, Object.keys(extra).join(','), Object.getOwnPropertyNames(extra).join(','));
Object.seal(extra);
console.log('sealed', Object.isSealed(extra), Object.isFrozen(extra));
extra.lastIndex = 3;
console.log('lastIndex writable', extra.lastIndex, Reflect.defineProperty(extra, 'newKey', {value: 1}));
console.log('frozen search', 'ba'.search(ordinary));
try {'ba'.search(regex);} catch (error) {console.log('search reset', (error as Error).name);}
const lockedGlobal = Object.freeze(/a/g);
try {'a'.search(lockedGlobal);} catch (error) {console.log('search execution', (error as Error).name);}
try {'a'.match(lockedGlobal);} catch (error) {console.log('match update', (error as Error).name);}
try {'a'.replace(lockedGlobal, 'b');} catch (error) {console.log('replace update', (error as Error).name);}
console.log('split copy', 'a,b'.split(Object.freeze(/,/)).join('|'));
