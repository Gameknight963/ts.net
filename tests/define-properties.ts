export {};
const value = {first: 1};
const symbol = Symbol('value');
console.log('identity', Object.defineProperties(value, {
    first: {value: 2, writable: false},
    second: {get() {return 3;}, enumerable: true},
    [symbol]: {value: 4, enumerable: true}
}) === value);
console.log('values', value.first, (value as any).second, (value as any)[symbol], Object.keys(value).join(','));
const untouched: any = {};
try {Object.defineProperties(untouched, {first: {value: 1}, bad: {get: 1 as any}});} catch (error) {console.log('validation', (error as Error).name, Object.getOwnPropertyNames(untouched).length);}
const partial: any = Object.create(null, {fixed: {value: 1}});
try {Object.defineProperties(partial, {first: {value: 2}, fixed: {value: 3}, last: {value: 4}});} catch (error) {console.log('application', (error as Error).name, partial.first, partial.fixed, Object.hasOwn(partial, 'last'));}
const alias = Object.defineProperties;
const other = {other: 5};
alias(other, {other: {get() {return 6;}}});
console.log('alias', other.other, Object.defineProperties.name, Object.defineProperties.length);
