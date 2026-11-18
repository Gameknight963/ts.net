export {};
const records: Record<string, unknown>[] = [];
const longKey = 'long'.repeat(100);
for (let i = 0; i < 10; i++) {
    const record: Record<string, unknown> = {};
    for (let j = 0; j < 160; j++) record['field' + j] = i + j;
    record[longKey] = i;
    record['é'] = i;
    record['你好'] = i + 1;
    Object.defineProperty(record, '__proto__', {value: {id: i}, enumerable: true});
    records.push(record);
}
const text = JSON.stringify(records);
let setters = 0;
Object.defineProperty(Object.prototype, 'field5', {set() {setters++;}, configurable: true});
const parsed = JSON.parse(text);
delete (Object.prototype as any).field5;
console.log('records', parsed.length, parsed[0].field159, parsed[9].field159, parsed[9]['é'], parsed[9]['你好']);
console.log('keys', Object.keys(parsed[0]).length, Object.keys(parsed[9]).slice(-3).join(','));
console.log('prototype', Object.getPrototypeOf(parsed[0]) === Object.prototype, Object.hasOwn(parsed[0], '__proto__'), parsed[9].__proto__.id, setters);
console.log('roundtrip', JSON.stringify(parsed) === text);

const duplicate = '[{"a":1,"b":2,"c":3,"d":4,"extra":5,"d":6,"extra":7},{"a":8,"b":9,"c":10,"d":11,"other":12}]';
console.log('duplicates', JSON.stringify(JSON.parse(duplicate)));
let visits = 0;
const revived = JSON.parse(text, (key, value) => {if (key === 'field159') visits++; return value;});
console.log('reviver', visits, revived[9].field159, Object.keys(revived[9]).length);
const varied = JSON.parse('[{"a":1,"b":2,"c":3,"d":4,"e":5},{"a":6,"b":7,"c":8,"d":9,"other":10},{"d":11,"e":12,"a":13,"b":14,"c":15}]');
console.log('varied order', varied.map((value: object) => Object.keys(value).join(',')).join('|'));
