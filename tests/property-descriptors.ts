function show(label: string, owner: any, key: any): void {
    const descriptor = Object.getOwnPropertyDescriptor(owner, key);
    if (!descriptor) { console.log(label, 'missing'); return; }
    console.log(label, Object.keys(descriptor).join(','), descriptor.writable, descriptor.enumerable, descriptor.configurable,
        'value' in descriptor ? typeof descriptor.value : typeof descriptor.get,
        'set' in descriptor ? typeof descriptor.set : 'data');
}
const ordinary = {value: 3};
show('ordinary', ordinary, 'value');
show('inherited', ordinary, 'toString');
show('missing', ordinary, 'missing');
const copy = Object.getOwnPropertyDescriptor(ordinary, 'value')!;
copy.value = 9; copy.enumerable = false;
console.log('copy', ordinary.value, Object.keys(ordinary).join(','));
let calls = 0;
const getter = function (): number { calls++; return 7; };
const setter = function (value: number): void { calls += value; };
const described = Object.create(null, {
    data: {value: 1, writable: true, enumerable: false, configurable: true},
    accessor: {get: getter, set: setter, enumerable: true, configurable: false},
    empty: {get: undefined, set: undefined},
    locked: {value: 5},
});
described.data = 2;
for (const key of ['data', 'accessor', 'empty', 'locked']) show(key, described, key);
const access = Object.getOwnPropertyDescriptor(described, 'accessor')!;
console.log('identity', access.get === getter, access.set === setter, calls);
console.log('live', Object.getOwnPropertyDescriptor(described, 'data')!.value);
const all = Object.getOwnPropertyDescriptors(described);
console.log('all', Object.keys(all).join(','), Object.keys(all.accessor).join(','), calls);
const literal = {get value(): number { throw new Error('getter called'); }};
show('literal accessor', literal, 'value');
class Example {
    field = 1;
    method(): void {}
    get value(): number { throw new Error('class getter called'); }
    static field = 2;
}
show('field', new Example(), 'field');
show('method', Example.prototype, 'method');
show('class accessor', Example.prototype, 'value');
show('class prototype', Example, 'prototype');
show('class name', Example, 'name');
show('static field', Example, 'field');
function ordinaryFunction(a: number): number { return a; }
show('function name', ordinaryFunction, 'name');
show('function length', ordinaryFunction, 'length');
show('function prototype', ordinaryFunction, 'prototype');
show('arrow prototype', () => 1, 'prototype');
const array = [1, , 3];
show('array item', array, '0'); show('array hole', array, '1'); show('array length', array, 'length');
show('primitive string', 'hi', '0'); show('string length', 'hi', 'length');
show('boxed string', Object('hi'), '1'); show('primitive number', 3, 'missing');
show('native method', Math, 'sin'); show('math constant', Math, 'PI');
show('number constant', Number, 'MAX_VALUE'); show('native prototype', Array, 'prototype');
show('API descriptor', Object, 'getOwnPropertyDescriptor');
console.log('metadata', Object.getOwnPropertyDescriptor.name, Object.getOwnPropertyDescriptor.length, Object.getOwnPropertyDescriptors.length);
function argumentsDescriptor(value: number): void {
    show('arguments item', arguments, '0'); show('arguments length', arguments, 'length');
    show('arguments callee', arguments, 'callee');
    const callee = Object.getOwnPropertyDescriptor(arguments, 'callee')!;
    console.log('thrower identity', callee.get === callee.set);
    try { callee.get!(); } catch (error) { console.log('thrower', (error as Error).name); }
}
argumentsDescriptor(4);
let conversions = 0;
const key = {toString(): string { conversions++; return 'value'; }};
show('coerced key', ordinary, key);
try { Object.getOwnPropertyDescriptor(null, key as any); } catch (error) { console.log('null', (error as Error).name, conversions); }
try { Object.getOwnPropertyDescriptors(undefined); } catch (error) { console.log('undefined', (error as Error).name); }
const special = {['__proto__']: 9};
console.log('special', Object.getOwnPropertyDescriptors(special).__proto__.value);
console.log('empty descriptors', Object.keys(Object.getOwnPropertyDescriptors(3)).length);

const mapped = Object.getOwnPropertyDescriptors(ordinary);
console.log('typed mapped', mapped.value.value, mapped.value.writable);
const annotated: PropertyDescriptor = {value: 3, writable: true};
console.log('annotated', annotated.value, annotated.writable);
try { (Number as any).MAX_VALUE = 1; } catch (error) { console.log('constant write', (error as Error).name); }
try { delete (Number as any).MAX_VALUE; } catch (error) { console.log('constant delete', (error as Error).name); }
console.log('constant retained', Number.MAX_VALUE > 1);
