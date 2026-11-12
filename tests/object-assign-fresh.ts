export {};
const source = {first: 1, second: 2, third: 3};
const copy = Object.assign({}, source);
console.log('plain copy', copy !== source, JSON.stringify(copy), Object.getPrototypeOf(copy) === Object.prototype);
function wider(input: {value: number}) {return Object.assign({}, input);}
console.log('wider copy', JSON.stringify(wider({value: 4, extra: 5} as any)));
const customPrototype = {inherited: 7};
const custom = {own: 6}; Object.setPrototypeOf(custom, customPrototype);
const customCopy = Object.assign({}, custom);
console.log('source prototype', customCopy.own, Object.getPrototypeOf(customCopy) === Object.prototype, 'inherited' in customCopy);
let reads = 0; const accessor = {get value() {reads++; return 8;}};
console.log('source accessor', Object.assign({}, accessor).value, reads);
// Obtain the definition function dynamically so the runtime guards are exercised.
const definitionName = ['define', 'Property'].join('');
const define = (Object as any)[definitionName];
const guarded = {guarded: 1, later: 2};
console.log('before setter', JSON.stringify(Object.assign({}, guarded)));
let observed: any;
define(Object.prototype, 'guarded', {set(this: any, value: number) {observed = this; this.seen = value; guarded.later = 9;}, configurable: true});
const setterCopy: any = Object.assign({}, guarded);
console.log('prototype setter', setterCopy === observed, setterCopy.seen, setterCopy.later, Object.keys(setterCopy).join(','));
const locked = {locked: 1};
console.log('before readonly', Object.assign({}, locked).locked);
define(Object.prototype, 'locked', {value: 99, writable: false, configurable: true});
try {Object.assign({}, locked);} catch(error) {console.log('prototype readonly', (error as Error).name);}
const getter = {getter: 1};
define(Object.prototype, 'getter', {get() {return 99;}, configurable: true});
try {Object.assign({}, getter);} catch(error) {console.log('prototype getter', (error as Error).name);}
const saved = Object.assign;
(Object as any).assign = function(this: any, target: any, input: any) {console.log('override', this === Object, Object.keys(target).length, input === source); return input;};
console.log('override result', Object.assign({}, source) === source);
const holder = {assign: saved};
console.log('borrowed native', JSON.stringify(holder.assign({}, source)));

(Object as any).assign = saved;
{
const source = {removable: 1};
const copy = Object.assign({}, source);
const method = ['delete', 'Property'].join('');
const remove = (Reflect as any)[method];
console.log('delete copied field', remove(copy, 'removable'), Object.keys(copy).join(','), copy.removable, source.removable);
}
{
const values = {zero: -0, nan: NaN, empty: undefined};
const copied = Object.assign({}, values);
console.log('copied primitive values', Object.is(copied.zero, -0), Number.isNaN(copied.nan), Object.hasOwn(copied, 'empty'), Object.keys(copied).join(','));
}
