const receiverNumber = {value: 3, valueOf(): number {return this.value;}};
console.log('receiver', +receiverNumber, Number(receiverNumber));
receiverNumber.value = 7;
console.log('updated', (receiverNumber as any) * 2);
let optionalNumber: number | undefined;
let numericCalls = 0;
const optionalObject: any = {valueOf: () => {numericCalls++; return optionalNumber;}};
for (const value of [undefined, -0, NaN, Infinity, 4]) {
    optionalNumber = value;
    const converted = +optionalObject;
    console.log('optional', Number.isNaN(converted), Object.is(converted, -0), converted === Infinity, converted === 4);
}
console.log('calls', numericCalls);
let defaultCalls = 0;
const defaultObject: any = {valueOf(value = (++defaultCalls, 9)): number {return value;}};
console.log('default', +defaultObject, defaultCalls);
const argumentObject: any = {value: 11, valueOf(): number {return this.value + arguments.length;}};
console.log('arguments', +argumentObject);
let conversionOrder = '';
const conversionGetters: any = {
    get [Symbol.toPrimitive]() {conversionOrder += 'S'; return undefined;},
    get valueOf() {conversionOrder += 'V'; return () => {conversionOrder += 'C'; return 13;};},
    get toString() {throw new Error('unexpected fallback');}
};
console.log('getters', +conversionGetters, conversionOrder);
let fallbackOrder = '';
const fallbackObject: any = {valueOf() {fallbackOrder += 'V'; return {};}, toString() {fallbackOrder += 'T'; return '0x10';}};
console.log('fallback', +fallbackObject, fallbackOrder);
const exoticObject: any = {[Symbol.toPrimitive](hint: string) {console.log('hint', hint); return 17;}, valueOf() {throw new Error('unexpected valueOf');}};
console.log('exotic', exoticObject * 2);
class NumericBase {value = 19; valueOf(): number {return this.value;}}
class NumericDerived extends NumericBase {override valueOf(): number {return super.valueOf() + 1;}}
console.log('class', +new NumericDerived());

let numberArgumentCalls = 0;
function numberArgument(): string {numberArgumentCalls++; return '23';}
console.log('direct', Number(numberArgument()), numberArgumentCalls, Number(), Number(undefined), Number(null), Number(true), Number('0x10'), Object.is(Number(-0), -0));
const numberAlias = Number;
console.log('alias', numberAlias(receiverNumber), numberAlias(17n));
function shadowNumber(Number: (value: string) => number): number {return Number('ignored');}
console.log('shadow', shadowNumber(() => 29));
console.log('spread', Number(...['0b10']), Number(...[]));
let ignoredArgumentCalls = 0;
console.log('extra', (Number as any)('31', ++ignoredArgumentCalls), ignoredArgumentCalls);
console.log('void', Number(console.log('void-argument')));
console.log('date', Number(new Date(0)));
