function compareStrings(left: string, right: string): void {
    console.log(left < right, left > right, left <= right, left >= right);
}
const strings = ['', '2', '10', 'a', 'aa', 'A', '\uD800', '\uDC00', '\uD83D\uDE00', '\uE000', '\u00E9', 'e\u0301'];
for (const left of strings) for (const right of strings) compareStrings(left, right);
const values: any[] = [undefined, null, false, true, 0, -0, NaN, Infinity, -Infinity, '', '2', '10', 'x', [], [2], [10], {valueOf: () => '2'}, {valueOf: () => 2}];
for (let i=0;i<values.length;i++) {
    for (let j=0;j<values.length;j++) {
        const left = values[i], right = values[j];
        console.log(i,j,left < right,left > right,left <= right,left >= right);
    }
}
let effects = '';
const left: any = {valueOf: () => {effects += 'L'; return '10';}};
const right: any = {valueOf: () => {effects += 'R'; return '2';}};
function first(): any {effects += 'A'; return left;}
function second(): any {effects += 'B'; return right;}
console.log(first() < second(), effects);
effects = '';
console.log(first() > second(), effects);
effects = '';
console.log(first() <= second(), effects);
effects = '';
console.log(first() >= second(), effects);
function absent(): string {return undefined as any;}
compareStrings(absent(), 'x');
compareStrings('x', absent());
