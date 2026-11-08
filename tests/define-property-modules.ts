import {define, objects, value, read} from './define-property-library';
define(value, 'moduleField', {get() {return 7;}, configurable: true});
console.log('imported', value.moduleField, read());
const other = {localField: 2};
objects.defineProperty(other, 'localField', {value: 8, writable: false});
try {other.localField = 9;} catch (error) {console.log('readonly', (error as Error).name);}
console.log('namespace', other.localField);
