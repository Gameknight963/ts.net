const values = [1, 2, 3];
console.log('map', values.map((value, index, source) => value + index + source.length).join(','));
console.log('filter', values.filter(value => value > 1).join(','));
let visited = '';
console.log('forEach', values.forEach((value, index) => {visited += index + ':' + value + ';';}), visited);
console.log('predicates', values.every(value => value > 0), values.some(value => value > 2));
console.log('empty', [].every(() => false), [].some(() => true));
const sparse: number[] = [1]; sparse.length = 3; sparse[2] = 3;
const mapped = sparse.map(value => value * 2);
console.log('holes', mapped.length, mapped.join('|'), Object.hasOwn(mapped, '1'), sparse.filter(() => true).join(','));
const mutating = [1, 2, 3];
console.log('mutation', mutating.map((value, index) => {if(index === 0) {mutating[1] = 8; mutating.push(4);} return value;}).join(','));
const shrinking = [1, 2, 3];
const shortened = shrinking.map((value, index) => {if(index === 0) shrinking.length = 1; return value;});
console.log('shrink', shortened.length, shortened.join('|'), Object.keys(shortened).join(','));
const retained = [{value: 1}];
const selected = retained.filter((value, index, source) => { source[index] = {value: 2}; return true; });
console.log('retain-original', selected[0].value, retained[0].value);
visited = '';
console.log('short-circuit', values.every(value => {visited += value; return value < 2;}), visited);
visited = '';
console.log('short-circuit-some', values.some(value => {visited += value; return value === 2;}), visited);
const context = {offset: 10};
console.log('receiver', values.map(function(this: typeof context, value) {return this.offset + value;}, context).join(','));
function callback(value: number, index: number, source: number[]): number {
    console.log('callback-arguments', arguments.length, arguments[2] === source, source === values);
    return value + index;
}
console.log('callback', values.map(callback).join(','));
const map: any = values.map, filter: any = values.filter, forEach: any = values.forEach;
const every: any = values.every, some: any = values.some;
const object = {0: 'a', 2: 'c', length: 3};
console.log('generic', map.call(object, (value: string, index: number, source: unknown) => value + index + (source === object)).join('|'));
const inherited: any = {length: 3}; inherited.__proto__ = {1: 'prototype'};
console.log('inherited', filter.call(inherited, () => true).join(','));
const array: any[] = []; array.length = 3; (array as any).__proto__ = {1: 'array-prototype'};
console.log('inherited-array', map.call(array, (value: string) => value).join('|'));
console.log('string', map.call('ab', (value: string, index: number, source: any) => value + index + typeof source + source.valueOf()).join('|'));
let boxed: any;
map.call('ab', (value: string, index: number, source: any) => { if (index === 0) boxed = source; else console.log('same-box', boxed === source); return value; });
console.log('boxed', Object.keys(boxed).join(','), Object.hasOwn(boxed, 'length'), boxed.length, '' + boxed);
try { boxed[0] = 'x'; } catch(error) { console.log('readonly', error instanceof TypeError); }
console.log('primitive-empty', map.call(42, () => 1).length, filter.call(true, () => true).length);
function argumentsSource(): string { return map.call(arguments, (value: number, index: number) => value + index).join(','); }
console.log('arguments-source', (argumentsSource as any)(5, 6));
for (const method of [map, filter, forEach, every, some]) {
    console.log('metadata', method.name, method.length);
    try {method.call([], undefined);} catch(error) {console.log('bad-callback', error instanceof TypeError);}
    try {method.call(null, () => true);} catch(error) {console.log('bad-receiver', error instanceof TypeError);}
}
let order = '';
const lengthGetter = {get length(): number {order += 'L'; return 0;}};
try {map.call(lengthGetter, undefined);} catch(error) {console.log('order', order, error instanceof TypeError);}
try {values.map(() => {throw 'callback failure';});} catch(error) {console.log('thrown', error);}
const mixed: (number | string)[] = [1, 'two', 3];
console.log('type-guard', mixed.filter((value): value is number => typeof value === 'number').map(value => value + 1).join(','));
console.log('object-results', values.map(value => ({value}))[1].value);
let presence = '';
const getters: any = {length: 3, get 0(): number {presence += 'A'; this[1] = 7; return 1;}, get 2(): number {presence += 'C'; return 3;}};
console.log('getters', map.call(getters, (value: number) => {presence += 'B'; return value;}).join(','), presence);
const holes = [1, 2, 3];
holes.length = 1; holes.length = 3;
let calls = 0;
holes.forEach(() => calls++);
console.log('empty-slots', calls, holes.every(() => true), holes.some(value => value > 1));
function strictThis(this: unknown): boolean { return this === undefined; }
console.log('strict-this', values.map(strictThis).join(','));
const Bound = function(this: typeof context, prefix: number, value: number): number { return this.offset + prefix + value; }.bind(context, 5);
console.log('bound', values.map(Bound, {offset: 100}).join(','));
{
    const shrinking=[1,2,3,4];
    const mapped=shrinking.map((value,index,array)=>{if(index===0)array.length=1;return value*2;});
    console.log('reserved result holes',mapped.length,Object.keys(mapped).join(','),mapped[0],mapped[1]);
    const sparse:number[]=[1];sparse.length=10000;sparse[9999]=3;
    const sparseResult=sparse.map(value=>value*2);
    console.log('sparse result holes',sparseResult.length,Object.keys(sparseResult).join(','),sparseResult[0],sparseResult[9999]);
}
for (const prototype of [Array.prototype, Object.prototype]) {
    const prototypeLength = Array.prototype.length;
    const source = [1];
    source.length = 6;
    source.map(value => value); // Exercise the cached absence check first.
    let visited = '';
    try {
        const result = source.map((value, index) => {
            visited += index + ':' + value + ';';
            if (index === 0) Object.defineProperty(prototype, '3', {get() {return 7;}, configurable: true});
            if (index === 3) delete (prototype as any)[3];
            return value * 2;
        });
        console.log('prototype changes during map', visited, result.length, Object.keys(result).join(','), result[3]);
    } finally {
        delete (prototype as any)[3];
        Array.prototype.length = prototypeLength;
    }
}
{
    const source: number[] = [1];
    source.length = 10000;
    source[9999] = 2;
    let visited = '';
    source.forEach((value, index) => {
        visited += index + ':' + value + ';';
        if (index === 0) {source[2500] = 3; source[7500] = 4;}
        if (index === 2500) {delete source[7500]; source[5000] = 5;}
        if (index === 5000) {source.length = 6000; source[5001] = 6;}
    });
    console.log('sparse callback mutations', visited, source.length, Object.keys(source).join(','));
    const defined: number[] = [1];
    defined.length = 100;
    defined[10] = undefined as any;
    defined[11] = NaN;
    let seen = '';
    defined.forEach((value, index) => {seen += index + ':' + value + ';';});
    console.log('present undefined and NaN', seen);
    let finds = 0;
    defined.find((value, index) => {finds++; return index === 12;});
    console.log('find visits holes', finds);
    const dense: number[] = [];
    for (let i = 0; i < 128; i++) dense.push(i);
    for (let i = 1; i < 128; i++) delete dense[i];
    let denseVisits = '';
    dense.forEach((value, index) => {
        denseVisits += index + ':' + value + ';';
        if (index === 0) dense[64] = 8;
    });
    console.log('holes in dense storage', denseVisits);
    const many: number[] = [1];
    many.length = 10000;
    for (let i = 1; i <= 64; i++) many[i * 100] = i;
    let count = 0, sum = 0;
    many.forEach(value => {count++; sum += value;});
    console.log('many sparse entries', count, sum);
    const boundaries: number[] = [];
    for (let i = 0; i <= 256; i++) boundaries.push(i);
    for (let i = 0; i <= 256; i++) if (i % 31 !== 0 && i !== 256) delete boundaries[i];
    let boundaryVisits = '';
    boundaries.forEach((value, index) => {boundaryVisits += index + ':' + value + ';';});
    console.log('hole search boundaries', boundaryVisits);
}
