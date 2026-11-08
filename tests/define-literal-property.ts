export {};
const value = {field: 1, other: 2};
Object.defineProperty(value, 'field', {get() {return 7;}, configurable: true});
console.log('getter', value.field, JSON.stringify(value));
Object.defineProperty(value, 'field', {value: 8, writable: false});
try {value.field = 9;} catch (error) {console.log('readonly', (error as Error).name);}
console.log('data', value.field, JSON.stringify(Object.getOwnPropertyDescriptor(value, 'field')));
const other = {first: 1};
const define = Object.defineProperty;
define(other, 'added', {value: 3, enumerable: true});
console.log('added', (other as any).added, Object.keys(other).join(','));
const {defineProperty: boundDefine} = Object;
const third = {third: 4};
boundDefine.call(Object, third, 'third', {get() {return 5;}});
console.log('alias', third.third);
function change(target: any, key: string): void {Object.defineProperty(target, key, {value: 9});}
const fourth = {value: 6};
change(fourth, 'value');
console.log('unknown', fourth.value);
