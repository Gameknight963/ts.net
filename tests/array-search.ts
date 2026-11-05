const numbers: number[] = [1, 2, 3, 2, 0 / 0, -0];
console.log(numbers.includes(2), numbers.indexOf(2), numbers.indexOf(2, 2));
console.log(numbers.includes(0 / 0), numbers.indexOf(0 / 0), numbers.indexOf(0), numbers.indexOf(-0));
console.log(numbers.includes(2, -3), numbers.indexOf(2, -3), numbers.indexOf(2, -99));
console.log(numbers.indexOf(2, 1.9), numbers.indexOf(2, -4.9), numbers.indexOf(2, 0 / 0));
console.log(numbers.includes(2, 1 / 0), numbers.indexOf(2, 1 / 0), numbers.indexOf(2, -1 / 0));
const missing = undefined as unknown as number;
const numericHoles: number[] = [1];
numericHoles.length = 3;
numericHoles[2] = 3;
console.log(numericHoles.includes(missing), numericHoles.indexOf(missing));
numericHoles[2] = missing;
console.log(numericHoles.includes(missing), numericHoles.indexOf(missing), numericHoles.indexOf(missing, 3));
const wide: any = numericHoles;
console.log(wide.includes(undefined), wide.indexOf(undefined), wide.includes(null), wide.indexOf("1"));
const generic: any[] = [1, , undefined, "hello", true, null, 0 / 0, -0];
console.log(generic.includes(undefined), generic.indexOf(undefined), generic.indexOf(undefined, 3));
console.log(generic.includes(0 / 0), generic.indexOf(0 / 0), generic.indexOf(0));
console.log(generic.includes("hello"), generic.indexOf(true), generic.indexOf(null));
console.log(generic.indexOf("1"), generic.indexOf(false), generic.includes(1));
console.log(generic.includes(missing), generic.indexOf(missing), generic.indexOf(1));
const dynamic: any = generic;
console.log(dynamic.includes(), dynamic.indexOf(), dynamic.indexOf("hello", "2"));
console.log(dynamic.includes(...[]), dynamic.indexOf(...["hello", "2"]));
console.log(numbers.includes(...[2, -3] as const), numbers.indexOf(...[2, 2] as const));
const empty: any[] = [];
console.log(empty.includes(undefined), empty.indexOf(undefined), empty.indexOf(0 / 0));
const point = { x: 1 };
const other = { x: 1 };
const objects = [point, other, point];
console.log(objects.includes(point), objects.indexOf(other), objects.indexOf(point, 1));
console.log(objects.includes({ x: 1 }), objects.indexOf({ x: 1 }));
const strings = ["he" + "llo", "world"];
console.log(strings.includes("hello"), strings.indexOf("hello"));
const sparse: any[] = [];
sparse[1000000] = 7;
sparse[500000] = 7;
console.log(sparse.indexOf(7), sparse.indexOf(undefined), sparse.includes(undefined));
sparse[4294967294] = 9;
console.log(sparse.indexOf(9, -2), sparse.includes(9, -2), sparse.indexOf(undefined, -2));
console.log(sparse.includes(undefined, -2), sparse.indexOf(7, -3), sparse.indexOf(9, -1));
const sparseNumbers: number[] = [];
sparseNumbers[1000000] = 7;
sparseNumbers[500000] = 7;
console.log(sparseNumbers.indexOf(7), sparseNumbers.includes(missing), sparseNumbers.indexOf(missing));
sparseNumbers[4294967294] = missing;
console.log(sparseNumbers.indexOf(missing, -2), sparseNumbers.includes(missing, -2));
let current: any = [1, 2];
function rebind() { current = [3, 4]; return 2; }
console.log(current.includes(rebind()), current.indexOf(2));
const state = { x: 0 };
function effect(value: number) { state.x = state.x * 10 + value; return value; }
console.log(dynamic.includes(effect(1), effect(2), effect(3)), state.x);
state.x = 0;
console.log(dynamic.indexOf(...[effect(1), effect(2), effect(3)]), state.x);

function searchGrid() {
    let checksum = 0;
    for (let length = 0; length < 40; length++) {
        const values: number[] = [];
        for (let i = 0; i < length; i++) values[i] = i % 7;
        for (let target = 0; target < 9; target++) {
            for (let from = -42; from < 43; from++) {
                checksum += values.indexOf(target, from);
                if (values.includes(target, from)) checksum++;
            }
        }
    }
    return checksum;
}
console.log(searchGrid());
const special: number[] = [1 / 0, -1 / 0, 0 / 0, -0, 0, 1];
special.length = 65;
console.log(special.indexOf(1 / 0), special.indexOf(-1 / 0), special.indexOf(0), special.indexOf(0, 4));
console.log(special.includes(0 / 0), special.indexOf(0 / 0), special.indexOf(missing), special.includes(missing));
special[64] = 0 / 0;
console.log(special.includes(0 / 0, 63), special.indexOf(0 / 0, 63));
let spreadReceiver: any = [1, 2];
function rebindSpread() { spreadReceiver = [3, 4]; return 2; }
console.log(spreadReceiver.indexOf(...[rebindSpread()]), spreadReceiver.includes(2));
