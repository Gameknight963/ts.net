export {};
let calls = 0;
class Custom extends RegExp {
    exec(text: string): RegExpExecArray | null {
        calls++;
        return super.exec(text);
    }
}
const regex = new Custom('a', 'g');
const iterator = 'aba'.matchAll(regex);
console.log('lazy', calls, regex.lastIndex, RegExp[Symbol.species] === RegExp, Custom[Symbol.species] === Custom);
console.log('matches', Array.from(iterator, match => match.index).join(','), calls, regex.lastIndex);
console.log('tag', Object.prototype.toString.call(iterator));
const method = RegExp.prototype[Symbol.matchAll];
let effects = '';
class Matcher {
    lastIndex = 0;
    constructor(source: unknown, flags: string) { effects += 'C'; console.log('construct', source === sourceObject, flags); }
    exec(text: string): any {
        console.log('exec', text, this.lastIndex);
        if (this.lastIndex > text.length) return null;
        return {0: '', index: this.lastIndex, length: 1};
    }
}
const sourceObject: any = {
    get constructor() { effects += 'K'; return {get [Symbol.species]() {effects += 'S'; return Matcher;}}; },
    get flags() { effects += 'F'; return 'gu'; },
    get lastIndex() { effects += 'L'; return {valueOf() {effects += 'N'; return 0;}}; }
};
const generic = method.call(sourceObject, '😀x');
console.log('order', effects);
for (const match of generic) console.log('generic', match.index);
console.log('done', generic.next().done);
for (const constructor of [null, 1, {[Symbol.species]: 42}]) {
    try { method.call({constructor, flags: 'g', lastIndex: 0} as any, 'x'); }
    catch (error) {console.log('invalid', (error as Error).name);}
}
const descriptor = Object.getOwnPropertyDescriptor(RegExp, Symbol.species)!;
console.log('descriptor', typeof descriptor.get, descriptor.set, descriptor.enumerable, descriptor.configurable);
let attempts = 0;
class Retry {
    lastIndex = 0;
    exec(): any {
        attempts++;
        if (attempts === 1) throw new Error('retry');
        return {0: 'value', length: 1};
    }
}
const retry = method.call({constructor: {[Symbol.species]: Retry}, flags: '', lastIndex: 0} as any, 'x');
try { retry.next(); } catch (error) { console.log('throw', (error as Error).message); }
console.log('retry', retry.next().value![0], retry.next().done, attempts);
