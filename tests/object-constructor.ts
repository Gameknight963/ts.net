const empty = Object();
const constructed = new Object();
console.log('empty', typeof empty, typeof constructed, Object.keys(empty).length, empty !== constructed, empty.constructor === Object);
console.log('nullish', Object.keys(Object(null)).length, Object.keys(Object(undefined)).length, Object.keys(new Object(null)).length);
const value = {field: 7};
const array = [1, 2];
const fn = () => 3;
console.log('identity', Object(value) === value, new Object(value) === value, Object(array) === array, Object(fn) === fn);
const text: any = Object('ab');
console.log('text', typeof text, text.length, text[0], text[1], text.valueOf(), '' + text, Object.keys(text).join(','));
console.log('text-own', Object.hasOwn(text, 'length'), Object.hasOwn(text, '0'), Object.hasOwn(text, 'valueOf'));
try {text[0] = 'x';} catch(error) {console.log('readonly-index', error instanceof TypeError);}
try {text.length = 5;} catch(error) {console.log('readonly-length', error instanceof TypeError);}
text.extra = 9;
console.log('extra', text.extra, Object.keys(text).join(','), Object(text) === text);
const number: any = Object(42);
const boolean: any = new Object(false);
console.log('primitives', typeof number, number.valueOf(), number.toString(), typeof boolean, boolean.valueOf(), boolean.toString(), Object.keys(number).length);
console.log('distinct-boxes', Object(1) !== Object(1), Object(false) !== Object(false));
const alias = Object;
console.log('alias', alias(value) === value, new alias(value) === value, alias.call(null, value) === value, alias.apply(null, [value]) === value);
const bound = Object.bind(null, value);
console.log('bound', bound() === value, new (bound as any)() === value);
console.log('instanceof', empty instanceof Object, value instanceof Object, array instanceof Object, text instanceof Object, number instanceof Object);
class Child extends Object {
    field = 5;
    #secret = 9;
    target: unknown;
    constructor(argument: object) {super(argument); this.target = new.target;}
    read(): number {return this.field + this.#secret;}
}
const child = new Child(value);
console.log('subclass', child !== value, child instanceof Child, child instanceof Object, child.constructor === Child, child.target === Child, child.read());
class Grandchild extends Child {}
const grandchild = new Grandchild(value);
console.log('grandchild', grandchild instanceof Child, grandchild instanceof Grandchild, grandchild.target === Grandchild, grandchild.read());
class Implicit extends Object {field = 8;}
const implicit = new Implicit(value);
console.log('implicit', implicit !== value, implicit instanceof Implicit, implicit.field, (implicit as any).field === 8);
const data: any = Object(); data.value = 10;
console.log('writable', data.value, Object.keys(data).join(','));
console.log('metadata', Object.name, Object.length, Object.prototype.constructor === Object);
console.log('number-conversion', Number(Object(42)), Number(Object(false)), Number(Object('12')));
let effects = 0;
const untouched = {toString(): string {effects++;return 'value';}, valueOf(): number {effects++;return 3;}};
console.log('no-conversion', Object(untouched) === untouched, new Object(untouched) === untouched, effects);
const primitiveChild = new Child(42 as any);
console.log('primitive-super', primitiveChild instanceof Child, primitiveChild.read());
const nullPrototype: any = {}; nullPrototype.__proto__ = null;
console.log('null-prototype', Object(nullPrototype) === nullPrototype, new Object(nullPrototype) === nullPrototype, Object(nullPrototype) instanceof Object);
console.log('prototype-identity', Object(Object.prototype) === Object.prototype, Object.prototype instanceof Object);
