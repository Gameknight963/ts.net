export {};
const value = {field: 1};
console.log('define', Reflect.defineProperty(value, 'field', {value: 2, configurable: false, writable: false}));
console.log('same', Reflect.defineProperty(value, 'field', {value: 2}));
for (const descriptor of [{value: 3}, {writable: true}, {configurable: true}, {enumerable: false}, {get() {return 4;}}]) console.log('reject', Reflect.defineProperty(value, 'field', descriptor));
console.log('unchanged', value.field);
const define = Reflect.defineProperty;
const other = {other: 5};
console.log('alias', define(other, 'other', {get() {return 6;}}), other.other);
try {Reflect.defineProperty(value, 'bad', {get: 1 as any});} catch (error) {console.log('invalid', (error as Error).name);}
let effects = '';
const key: any = {toString() {effects += 'K'; return 'x';}};
try {Reflect.defineProperty(null as any, key, {});} catch (error) {console.log('target', (error as Error).name, effects);}
try {Reflect.defineProperty(other, key, {get value() {effects += 'V'; throw new Error('descriptor');}});} catch (error) {console.log('descriptor', (error as Error).message, effects);}
console.log('metadata', Reflect.defineProperty.name, Reflect.defineProperty.length);
