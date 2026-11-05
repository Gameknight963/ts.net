let effects = '';
const left: any = {valueOf: () => { effects += 'L'; return 8; }, toString: () => { effects += 'S'; return 'wrong'; }};
const right: any = {valueOf: () => { effects += 'R'; return 2; }};
function rhs(): any { effects += 'E'; return right; }
console.log(left - rhs(), effects);
effects = '';
console.log(left ** rhs(), effects);
effects = '';
console.log(left << rhs(), effects);
effects = '';
let variable: any = left;
console.log(variable **= rhs(), effects, variable);
effects = '';
const slot: any = {value: left as any};
console.log(slot.value *= rhs(), effects, slot.value);
effects = '';
console.log(left + rhs(), effects);
const stringValue: any = {valueOf: () => '12'};
console.log(stringValue + 3, +stringValue, stringValue * 2);
console.log('x' + left, left + 'x');
let text = 'x';
text += left;
console.log(text);
effects = '';
const fallback: any = {valueOf: () => { effects += 'V'; return {}; }, toString: () => { effects += 'T'; return '0x10'; }};
console.log(+fallback, effects);
const noncallable: any = {valueOf: 9, toString: () => '17'};
console.log(+noncallable);
const primitiveResults: any[] = [{valueOf: () => null}, {valueOf: () => undefined}, {valueOf: () => true}, {valueOf: () => -0}];
for (const value of primitiveResults) { const n = +value; console.log(n, n !== n, n === 0 && 1 / n < 0); }
const arrays: any[] = [[], [null], [undefined], ['0b11'], [1,2], [[4]]];
for (const value of arrays) console.log(+value);
const customJoin: any = [];
customJoin.join = () => '21';
console.log(+customJoin);
const cycle: any[] = [];
cycle.push(cycle);
console.log(+cycle);
effects = '';
let changing: any = {valueOf: () => { effects += 'old'; return 3; }};
function change(): number { effects += 'rhs'; changing = {valueOf: () => 99}; return 2; }
console.log(changing ** change(), effects);
effects = '';
const first: any = {valueOf: () => { effects += 'A'; return 2; }};
const second: any = {valueOf: (() => 3) as any};
const mutator: any = {valueOf: () => { effects += 'B'; second.valueOf = () => { effects += 'C'; return 4; }; return 2; }};
console.log(mutator * second, effects);

function shrinkingArray(): string {
    const values: any[] = [1,2,3,4];
    const coercer: any = {valueOf: () => {values.length = 0; return 0;}};
    let result = '';
    for (let i=0;i<4;i++) { +coercer; result += typeof values[i]; }
    return result;
}
console.log(shrinkingArray());
