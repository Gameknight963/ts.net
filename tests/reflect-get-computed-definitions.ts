export {};
const target = {value: 1};
const define = Reflect.get(Object, ['define', 'Property'].join(''));
define(target, 'value', {value: 7, writable: false, enumerable: false, configurable: false});
console.log(target.value, Object.keys(target).join(','));
let threw = false;
try {target.value = 9;} catch {threw = true;}
console.log(threw, target.value);
const other = {value: 3};
const prevent = Reflect.get(Object, ['prevent', 'Extensions'].join(''));
prevent(other);
console.log(Object.isExtensible(other));
threw = false;
try {(other as any).extra = 7;} catch {threw = true;}
console.log(threw, Object.hasOwn(other, 'extra'));
