export {};
const map = new Map<string, number>();
const augmented = Object.assign(map, {label: 'map'});
augmented.set('a', 1);
console.log(augmented.label, augmented.size, augmented === map);
interface Named {label: string;}
function name(value: Named) {return value.label;}
console.log(name(augmented));
function freeze<T extends object>(value: T): Readonly<T> {return Object.freeze(value);}
const error = freeze(new Error('generic'));
console.log(error.message, Object.isFrozen(error));
function attach<T extends object>(value: T) {return Object.assign(value, {name: 'attached'});}
const date = attach(new Date(1000));
console.log(date.name, date.getTime());
const readonlyDate: Readonly<Date> = new Date(2000);
console.log('readonly date', readonlyDate.getTime());
const partial: Partial<Error> = new Error('partial');
console.log('partial error', partial.message);
type ErrorText = Pick<Error, 'name' | 'message'>;
function errorText(value: ErrorText) {return value.name + ':' + value.message;}
console.log('picked error', errorText(error));
type NumericFields<T> = {[K in keyof T]: number};
const fields: NumericFields<Error> = {name: 1, message: 2};
fields.message += 3;
console.log('changed property types', fields.name, fields.message);
