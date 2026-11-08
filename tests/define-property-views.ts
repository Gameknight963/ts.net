export {};
interface View {field: number;}
function read(value: View): number {return value.field;}
function write(value: View, next: number): void {value.field = next;}
const value = {field: 1, other: 2};
const view: View = value;
let stored = 7, sets = 0;
Object.defineProperty(value, 'field', {get() {return stored;}, set(next: number) {sets++; stored = next;}, configurable: true});
console.log('read', value.field, view.field, read(value));
write(view, 8);
console.log('write', value.field, stored, sets);
Object.defineProperty(value, 'field', {value: 9, writable: false});
try {write(view, 10);} catch (error) {console.log('readonly', (error as Error).name);}
console.log('final', read(view), Object.keys(value).join(','));
const other = {field: 3};
const define = Object.defineProperty.bind(Object);
define(other, 'field', {get() {return 11;}});
console.log('bound alias', read(other));
