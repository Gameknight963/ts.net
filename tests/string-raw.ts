console.log('basic', String.raw({raw: ['a', 'b', 'c']}, 1, 2));
console.log('missing', String.raw({raw: ['a', 'b', 'c']}, 1));
console.log('undefined', String.raw({raw: ['a', 'b']}, undefined));
console.log('empty', String.raw({raw: []}, {toString(): never {throw 'unused';}}));
console.log('extra', String.raw({raw: ['one']}, {toString(): never {throw 'unused';}}));
console.log('escapes', String.raw({raw: ['\\n', '\\t']}, 'x'));
const raw = String.raw as any;
console.log('string raw', raw({raw: 'abc'}, 1, 2));
console.log('array like', raw({raw: {length: 3, 0: 'x', 2: 'z'}}, '-', '-'));
for (const length of [-1, NaN, 0, 1.9, '2']) console.log('length', length, raw({raw: {length, 0: 'a', 1: 'b'}}, '!'));
const source: any = {length: 3, 0: 'a', 1: 'b', 2: 'c'};
const substitution = {toString(): string {console.log('substitution');source.length = 1; source[1] = 'changed';return '!';}};
console.log('live', raw({raw: source}, substitution, '?'));
const template = {get raw(): any {console.log('raw getter');return {get length(): number {console.log('length getter');return 2;}, get 0(): any {console.log('first getter');return {toString(): string {console.log('first conversion');return 'x';}};}, get 1(): string {console.log('second getter');return 'y';}};}};
console.log('order', raw(template, {toString(): string {console.log('middle conversion');return '-';}}));
for (const template of [null, undefined, {}, {raw: null}, {raw: undefined}]) {
    try {raw(template);} catch(error) {console.log('invalid', (error as Error).name);}
}
console.log('primitive raw', raw({raw: 1}), raw({raw: false}));
try {raw({raw: [{toString(): never {throw 'first failure';}}, 'later']}, substitution);} catch(error) {console.log('failure', error);}
console.log('borrowed', String.raw.call(null, {raw: ['a', 'b']}, 3));
console.log('metadata', String.raw.name, String.raw.length, Object.keys(String).includes('raw'));
