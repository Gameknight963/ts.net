export {};
class Numeric extends Number {value = 7; #secret = 8; read() {return this.#secret;}}
class Textual extends String {value = 7; #secret = 8; read() {return this.#secret;}}
class Logical extends Boolean {value = 7; #secret = 8; read() {return this.#secret;}}
class Calendar extends Date {value = 7; #secret = 8; read() {return this.#secret;}}
class Pattern extends RegExp {value = 7; #secret = 8; read() {return this.#secret;}}
class Dictionary extends Map<string, number> {value = 7; #secret = 8; read() {return this.#secret;}}
class Unique extends Set<number> {value = 7; #secret = 8; read() {return this.#secret;}}
class Failure extends Error {value = 7; #secret = 8; read() {return this.#secret;}}
class Buffer extends ArrayBuffer {value = 7; #secret = 8; read() {return this.#secret;}}
class Bytes extends Uint8Array {value = 7; #secret = 8; read() {return this.#secret;}}
const cases: any[] = [new Numeric(42), new Textual('text'), new Logical(true), new Calendar(0), new Pattern('a', 'g'), new Dictionary([['key', 42]]), new Unique([42]), new Failure('message'), new Buffer(4), new Bytes([1, 2, 3])];
for (const value of cases) {
    console.log('fields', value.constructor.name, value.value, value.read(), Object.hasOwn(value, 'value'), Object.keys(value).includes('value'));
    const descriptor = Object.getOwnPropertyDescriptor(value, 'value')!;
    console.log('descriptor', descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
console.log('native', cases[0].valueOf(), cases[1].toString(), cases[2].valueOf(), cases[3].getUTCFullYear(), cases[4].test('a'), cases[5].get('key'), cases[6].has(42), cases[7].message, cases[8].byteLength, cases[9].join(','));
class StringLength extends String {length = 9;}
class StringIndex extends String {0 = 'replacement';}
class RegExpIndex extends RegExp {lastIndex = 9;}
for (const constructor of [StringLength, StringIndex, RegExpIndex]) {
    try {new constructor('text'); console.log('collision allowed', constructor.name);}
    catch (error) {console.log('collision', constructor.name, (error as Error).name);}
}
class BufferLength extends ArrayBuffer {byteLength = 9;}
class ByteLength extends Uint8Array {byteLength = 9;}
console.log('inherited getter', new BufferLength(3).byteLength, new ByteLength(3).byteLength);

class View extends DataView<ArrayBuffer> {value = 7; #secret = 8; read() {return this.#secret;}}
class Shared extends SharedArrayBuffer {value = 7; #secret = 8; read() {return this.#secret;}}
class WeakDictionary extends WeakMap<object, number> {value = 7; #secret = 8; read() {return this.#secret;}}
class WeakUnique extends WeakSet<object> {value = 7; #secret = 8; read() {return this.#secret;}}
class Failures extends AggregateError {value = 7; #secret = 8; read() {return this.#secret;}}
const key = {};
const view = new View(new ArrayBuffer(8));
view.setInt32(0, 123);
const shared = new Shared(8);
const weakMap = new WeakDictionary([[key, 42]]);
const weakSet = new WeakUnique([key]);
const failures = new Failures([1, 2], 'both');
for (const value of [view, shared, weakMap, weakSet, failures]) {
    const descriptor = Object.getOwnPropertyDescriptor(value, 'value')!;
    console.log('more fields', value.constructor.name, value.value, value.read(), descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
console.log('more native', view.getInt32(0), shared.byteLength, weakMap.get(key), weakSet.has(key), failures.errors.join(','), failures.message);

class Reference extends WeakRef<object> {value = 7; #secret = 8; read() {return this.#secret;}}
class Registry extends FinalizationRegistry<string> {value = 7; #secret = 8; read() {return this.#secret;}}
const reference = new Reference(key);
const registry = new Registry(() => {});
registry.register(key, 'held', key);
console.log('weak subclasses', reference.value, reference.read(), reference.deref() === key, registry.value, registry.read(), registry.unregister(key));

class Callable extends Function {value = 7; #secret = 8; read() {return this.#secret;}}
const callable = new Callable();
console.log('callable fields', typeof callable, (callable as any)(), callable.value, callable.read(), callable instanceof Callable, callable instanceof Function);
class NamedCallable extends Function {name = 'renamed'; length = 4;}
const renamed = new NamedCallable();
console.log('callable metadata fields', renamed.name, renamed.length, (renamed as any)(), Object.keys(renamed).join(','));
class FunctionPrototypeField extends Function {prototype = {};}
try {new FunctionPrototypeField(); console.log('function prototype field allowed');}
catch (error) {console.log('function prototype field', (error as Error).name);}
