export {};
const value = {uniqueBoundField: 1};
const define = Object.defineProperty.bind(Object, value, 'uniqueBoundField');
define({get() {return 7;}, configurable: true});
console.log('bound key', value.uniqueBoundField);
const target = {anotherBoundField: 2};
const defineTarget = Object.defineProperty.bind(Object, target);
defineTarget('anotherBoundField', {value: 8, writable: false});
try {target.anotherBoundField = 9;} catch (error) {console.log('readonly', (error as Error).name);}
console.log('bound target', target.anotherBoundField);
