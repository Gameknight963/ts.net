const bases: number[] = [0, -0, 1, -1, 2, -2, 4, -4, Infinity, -Infinity, NaN];
const powers: number[] = [0, -0, 1, -1, 2, 3, -2, -3, 0.5, Infinity, -Infinity, NaN];
for (const base of bases) {
    for (const exponent of powers) {
        const result = base ** exponent;
        console.log(base, exponent, result, 1 / result);
    }
}
console.log(2 ** 3 ** 2, (-2) ** 3, 2 ** -3);
let local = 3;
console.log(local **= 3, local);
const obj = {value: 2};
console.log(obj.value **= 5, obj.value);
const values = [3];
console.log(values[0] **= 2, values[0]);
let dynamic: any = '2';
console.log(dynamic ** 3, dynamic **= 4, dynamic);
const missing: any = undefined;
console.log(missing ** 0, missing ** 2, 2 ** missing);
let effects = '';
function receiver(): number[] { effects += 'R'; return values; }
function index(): number { effects += 'K'; return 0; }
function right(): number { effects += 'V'; values[0] = 4; return 2; }
console.log(receiver()[index()] **= right(), effects, values[0]);
function square(value: number): number { return value ** 2; }
console.log(square(7), square(-0), 1 / square(-0));
