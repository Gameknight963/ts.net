export {};
class Record {a = 1; b = 2; c = 3; d = 4; e = 5;}
const item = new Record();
for (let i = 0; i < 5; i++) {item.a = i; item.b += i; item.c++; item.d = i * 2; item.e = i * 3;}
console.log('values', item.a, item.b, item.c, item.d, item.e, Object.keys(item).join(','));
function write(value: {a: number}, number: number): number {return value.a = number;}
console.log('interface', write(item, 17), item.a);
const alias: any = item;
console.log('boxed', typeof alias.a, alias.a, Object.values(item).join(','), JSON.stringify(item));
const copy = {...item}; item.a = 19; copy.a = 23;
console.log('copy', item.a, copy.a);
alias.a = 'text'; console.log('other type', alias.a); item.a = 29;
console.log('number again', item.a);
item.a = -0; console.log('zero', Object.is(item.a, -0));
item.a = NaN; console.log('nan', Number.isNaN(item.a));
item.a = undefined as any; console.log('undefined', item.a, 'a' in item); item.a = 31;
delete alias.b; item.c = 37; item.e = 41;
console.log('shifted', item.a, item.c, item.e, Object.keys(item).join(','));
Object.defineProperty(item, 'a', {value: 43, writable: false});
try {item.a = 47;} catch {console.log('readonly');}
console.log('descriptor', item.a, Object.getOwnPropertyDescriptor(item, 'a')!.value);
Object.defineProperty(item, 'c', {get() {return 53;}, set(value: number) {console.log('setter', value);}, configurable: true});
item.c = 59; console.log('getter', item.c);
const frozen = new Record(); frozen.a = 60; frozen.a = 61; Object.freeze(frozen);
try {frozen.a = 67;} catch {console.log('frozen');}
console.log('unchanged', frozen.a);
class Parent {set value(value: number) {console.log('inherited setter', value);}}
class Child extends Parent {}
const child = new Child(); child.value = 71;

const dictionary: {a: number} = JSON.parse('{"a":1}');
console.log('dictionary', write(dictionary, 73), dictionary.a);
const created: any = Object.create(null); created.a = 1;
for (let i=0;i<5;i++) write(created,i);
console.log('created', created.a, Object.values(created).join(','));
