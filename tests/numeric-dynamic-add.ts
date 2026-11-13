export {};
function sum(value: any, initial: number): number { let total = initial; total += value; return total; }
for (const value of [2, -0, NaN, Infinity, undefined, null, true, false]) {
    const result = sum(value, -0);
    console.log(typeof value, result, Object.is(result, -0));
}
const events: string[] = [];
const converted = {valueOf() {events.push('convert'); return 7;}};
console.log('object', sum(converted, 3), events.join(','));
const target = {value: 2};
function rhs(): any {target.value = 100; return 3;}
target.value += rhs(); console.log('order', target.value);
let stored = 4;
const holder = {get value(): number {events.push('get'); return stored;}, set value(value: number) {events.push('set'); stored = value;}};
function operand(): any {events.push('rhs'); return 5;}
events.length = 0; holder.value += operand();
console.log('accessor', stored, events.join(','));
try {sum({valueOf() {throw new Error('conversion');}}, 1);} catch (error) {console.log((error as Error).message);}

console.log('missing accumulator', sum(2, undefined as any));
let local = 2; const expression: any = (local += operand());
console.log('expression', expression, local);
const indexed = [3, 4]; indexed[0] += operand();
console.log('index', indexed.join(','));
const untyped: any = 3; let zero = -0; zero += untyped;
console.log('number', zero);

const union: (number | string)[] = [1, 'text', 3];
let unionTotal = 0;
for (const value of union) {if (typeof value === 'number') unionTotal += value;}
console.log('union', unionTotal);
