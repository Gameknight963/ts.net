export {};
class LengthView {get length(): number {return 0;}}
function lengthView(value: unknown): LengthView {return value as LengthView;}
function plusOne(value: LengthView): number {return value.length + 1;}
const text: unknown = 'hello';
const length = lengthView(text);
console.log('string length', length.length, plusOne(length), typeof length, (length as unknown) === text, length instanceof LengthView);
console.log('array length', lengthView([1, 2, 3]).length);
console.log('class instance', lengthView(new LengthView()).length);
let reads = 0;
const accessor = {get length() {reads++; return 12;}};
const accessorView = lengthView(accessor);
console.log('no getter during cast', reads, accessorView === accessor);
console.log('getter read', accessorView.length, reads);
class NumberView {valueOf(): number {return 0;}}
function numberView(value: unknown): NumberView {return value as NumberView;}
console.log('number value', numberView(21).valueOf(), typeof numberView(21), Number.isNaN(numberView(NaN).valueOf()));
class BooleanView {valueOf(): boolean {return false;}}
function booleanView(value: unknown): BooleanView {return value as BooleanView;}
console.log('boolean value', booleanView(true).valueOf(), typeof booleanView(true));
class StringView {valueOf(): string {return '';}}
function stringView(value: unknown): StringView {return value as StringView;}
console.log('string value', stringView('word').valueOf(), typeof stringView('word'));
class BigIntView {valueOf(): bigint {return 0n;}}
function bigIntView(value: unknown): BigIntView {return value as BigIntView;}
console.log('bigint value', bigIntView(42n).valueOf() === 42n, typeof bigIntView(42n));
class SymbolView {valueOf(): symbol {return Symbol();}}
function symbolView(value: unknown): SymbolView {return value as SymbolView;}
const symbol = Symbol('original');
console.log('symbol value', symbolView(symbol).valueOf() === symbol, typeof symbolView(symbol));
const record: {view: LengthView} = {view: new LengthView()};
(record as any).view = text;
console.log('dynamic fixed field', record.view.length, (record.view as unknown) === text);
class Holder {view: LengthView = new LengthView();}
const holder = new Holder();
(holder as any).view = text;
console.log('dynamic class field', holder.view.length, (holder.view as unknown) === text);
console.log('nullish', lengthView(undefined) === undefined, lengthView(null) === null);
