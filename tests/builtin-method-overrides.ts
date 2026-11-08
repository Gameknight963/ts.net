export {};
const originalNaN = Number.isNaN, originalKeys = Object.keys;
Number.isNaN = value => value === 'override';
console.log('number direct', Number.isNaN('override'), Number.isNaN(NaN));
const {isNaN} = Number;
console.log('number binding', isNaN('override'), isNaN(NaN));
Number.isNaN = originalNaN;
console.log('number restored', Number.isNaN(NaN));
Object.keys = () => ['override'];
console.log('object direct', Object.keys({a: 1}).join(','));
const {keys} = Object;
console.log('object binding', keys({a: 1}).join(','));
Object.keys = originalKeys;
console.log('object restored', Object.keys({a: 1}).join(','));
const numeric = Number;
numeric.isFinite = value => value === 'finite';
console.log('number alias', Number.isFinite('finite'), Number.isFinite(1));
const object = Object;
object.values = () => ['values'];
console.log('object alias', Object.values({a: 1}).join(','));
const savedInteger = Number.isInteger;
Object.defineProperty(Number, 'isInteger', {value: (value: unknown) => value === 'integer', configurable: true, writable: true});
console.log('defined', Number.isInteger('integer'), Number.isInteger(1));
Number.isInteger = savedInteger;
delete (Object as any).keys;
try {Object.keys({a: 1});}
catch (error) {console.log('deleted', (error as Error).name);}
Object.keys = originalKeys;
console.log('after delete', Object.keys({b: 2}).join(','));
