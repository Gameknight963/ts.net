export {};
let calls = 0;
class Custom extends RegExp {
    exec(text: string): RegExpExecArray | null { calls++; return super.exec(text); }
}
const regex = new Custom('(,)', 'g');
regex.lastIndex = 50;
console.log('subclass', JSON.stringify('a,b,c'.split(regex)), calls, regex.lastIndex);
const method = RegExp.prototype[Symbol.split];
let effects = '';
class Matcher {
    lastIndex = 0;
    constructor(source: unknown, flags: string) { effects += 'C'; console.log('construct', source === sourceObject, flags); }
    exec(text: string): any {
        console.log('exec', text.length, this.lastIndex);
        return {0: '', length: 1};
    }
}
const sourceObject: any = {
    get constructor() { effects += 'K'; return {get [Symbol.species]() {effects += 'S'; return Matcher;}}; },
    get flags() { effects += 'F'; return 'u'; },
    get lastIndex() { throw new Error('must not read original lastIndex'); }
};
const limit: any = {valueOf() { effects += 'L'; return 9; }};
console.log('unicode', JSON.stringify(method.call(sourceObject, '😀x', limit)), effects);
console.log('empty', JSON.stringify(method.call(sourceObject, '')));
console.log('zero', JSON.stringify(method.call(sourceObject, 'x', 0)));
class Captures {
    lastIndex = 0;
    exec(text: string): any {
        if (text[this.lastIndex] !== ',') return null;
        this.lastIndex++;
        return {0: ',', 1: undefined, 2: {value: 42}, length: 3};
    }
}
const custom: any = {constructor: {[Symbol.species]: Captures}, flags: ''};
for (const limit of [undefined, 1, 2, 3, 4]) console.log('captures', JSON.stringify(method.call(custom, 'a,b,c', limit)));
