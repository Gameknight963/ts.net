function typed(value: {number?: number}): number | undefined { return value.number; }
function dynamic(value: any): any { return value.number; }
const methods = {number: 0, read(): number { return this.number; }};
function show(value: any): void { console.log(typed(value), dynamic(value), methods.read.call(value), Object.hasOwn(value, 'number')); }
const empty: {number?: number} = {};
const copied = {...empty};
Object.setPrototypeOf(copied, {number: 61});
show(copied);
Object.setPrototypeOf(copied, {get number(): number {console.log('receiver', this === copied); return 67;}});
show(copied);
Object.setPrototypeOf(copied, {number: 69});
copied.number = 71;
show(copied);
Object.setPrototypeOf(copied, null);
show(copied);
const ownUndefined = {...{number: undefined as number | undefined}};
Object.setPrototypeOf(ownUndefined, {number: 73});
show(ownUndefined);
const ownNaN = {...{number: NaN}};
Object.setPrototypeOf(ownNaN, {number: 79});
show(ownNaN);
const text: {word?: string} = {};
const copyText = {...text};
Object.setPrototypeOf(copyText, {word: 'inherited'});
function readText(value: {word?: string}): string | undefined { return value.word; }
console.log(readText(copyText), (copyText as any).word, Object.hasOwn(copyText, 'word'));
