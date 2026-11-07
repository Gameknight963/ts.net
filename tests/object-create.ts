const ancestor = {inherited: 3, get doubled(): number {return (this as any).own * 2;}};
const child = Object.create(ancestor);
child.own = 4;
console.log('prototype', Object.getPrototypeOf(child) === ancestor, child.inherited, child.doubled, Object.keys(child).join(','));
const dictionary = Object.create(null);
dictionary.__proto__ = 5;
console.log('null', Object.getPrototypeOf(dictionary) === null, dictionary.__proto__, 'toString' in dictionary, Object.keys(dictionary).join(','));
let backing = 10;
const defined = Object.create(ancestor, {
    fixed: {value: 7},
    mutable: {value: 8, writable: true, enumerable: true, configurable: true},
    read: {get(): number {return backing;}, enumerable: true},
    write: {set(value: number) {backing = value;}, enumerable: true},
    both: {get(): number {return (this as any).mutable;}, set(value: number) {(this as any).mutable = value;}, enumerable: true},
    emptyAccessor: {get: undefined, set: undefined},
});
console.log('defined', defined.fixed, defined.mutable, defined.read, defined.write, defined.emptyAccessor, Object.keys(defined).join(','));
defined.mutable = 9; defined.write = 11; defined.both = 12;
console.log('updated', defined.mutable, defined.read, defined.both);
for (const key of ['fixed', 'read', 'emptyAccessor']) {
    try {defined[key] = 20;} catch (error) {console.log('readonly', key, (error as Error).name);}
}
const descendant = Object.create(defined);
try {descendant.fixed = 2;} catch (error) {console.log('inherited readonly', (error as Error).name);}
descendant.both = 15;
console.log('receiver', descendant.mutable, defined.mutable);
const shadow = Object.create({hidden: 1}, {hidden: {value: 2}, visible: {value: 3, enumerable: true}});
for (const key in shadow) console.log('enumerated', key);
console.log('copy', Object.keys(Object.assign({}, defined)).join(','), Object.keys({...defined}).join(','));
console.log('function', Object.create.name, Object.create.length, Object.keys(Object).includes('create'));
for (const prototype of [undefined, 1, 'x', true]) {
    try {Object.create(prototype as any);} catch (error) {console.log('invalid prototype', (error as Error).name);}
}
for (const properties of [null, {x: 1}, {x: {get: 1}}, {x: {set: null}}, {x: {value: 1, get: undefined}}]) {
    try {Object.create(null, properties as any);} catch (error) {console.log('invalid descriptor', (error as Error).name);}
}
const order = {get enumerable(): boolean {console.log('enumerable');return true;}, get configurable(): boolean {console.log('configurable');return true;}, get value(): number {console.log('value');return 21;}, get writable(): boolean {console.log('writable');return true;}};
console.log('order', Object.create(null, {x: order}).x);
