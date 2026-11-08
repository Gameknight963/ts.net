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
