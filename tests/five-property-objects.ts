const propertyValues: any[] = [null, undefined, -0, NaN, 17n, Symbol('value'), 5, 'text', {nested: true}, [7]];
for (let index = 0; index < propertyValues.length; index++) {
    const record: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"e":null}');
    record.e = propertyValues[index];
    const copy = {...record};
    console.log('copy', index, Object.keys(record).join(','), Object.is(record.e, copy.e), Object.hasOwn(copy, 'e'));
    record.e = {replacement: index};
    console.log('independent', index, Object.is(copy.e, propertyValues[index]), record.e !== copy.e);
    delete record.e;
    console.log('delete', index, Object.keys(record).join(','), Object.keys(copy).join(','));
    record.f = 6;
    record.g = 7;
    delete record.b;
    record.b = 8;
    console.log('promote', index, Object.keys(record).join(','), record.f, record.g, record.b);
    delete record.f;
    record.e = propertyValues[index];
    console.log('reinsert', index, Object.keys(record).join(','), Object.is(record.e, copy.e));
}
const symbolRecord: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4}');
const fifthSymbol = Symbol('fifth');
symbolRecord[fifthSymbol] = undefined;
console.log('symbol', Reflect.ownKeys(symbolRecord).map(key => typeof key === 'symbol' ? 'symbol' : key).join(','), Object.hasOwn(symbolRecord, fifthSymbol));
const symbolCopy = {...symbolRecord};
delete symbolRecord.b;
console.log('symbol-promote', Object.keys(symbolRecord).join(','), Object.getOwnPropertySymbols(symbolRecord)[0] === fifthSymbol, Object.hasOwn(symbolCopy, fifthSymbol));
const descriptorRecord: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"e":5}');
Object.defineProperty(descriptorRecord, 'e', {get() {return 9;}, enumerable: true, configurable: true});
console.log('getter', descriptorRecord.e, Object.keys(descriptorRecord).join(','));
delete descriptorRecord.e;
descriptorRecord.f = 6;
Object.freeze(descriptorRecord);
console.log('freeze', Object.isFrozen(descriptorRecord), descriptorRecord.f, Object.keys(descriptorRecord).join(','));

const emptyKeyRecord: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"":null}');
console.log('empty-key', Object.hasOwn(emptyKeyRecord, ''), Object.keys(emptyKeyRecord).length, emptyKeyRecord[''] === null);
delete emptyKeyRecord.a;
console.log('empty-promote', Object.hasOwn(emptyKeyRecord, ''), Object.keys(emptyKeyRecord).join(','));
const protoRecord: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"__proto__":{"marker":true}}');
console.log('proto-own', Object.hasOwn(protoRecord, '__proto__'), Object.getPrototypeOf(protoRecord) === Object.prototype, protoRecord.__proto__.marker);

const numericKeyRecord: any = JSON.parse('{"a":1,"b":2,"c":3,"d":4,"0":5}');
console.log('numeric-key', Object.keys(numericKeyRecord).join(','), numericKeyRecord[0]);
const numericView: {a: number} = numericKeyRecord;
for (let index = 0; index < 3; index++) numericView.a++;
const numericCopy = {...numericKeyRecord};
numericView.a++;
console.log('numeric-copy', numericView.a, numericCopy.a, Object.keys(numericCopy).join(','));
