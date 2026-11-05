import { exported, result as moduleResult } from "./modules/void-values.js";
const callback: () => void = () => 42;
console.log(callback());
const observed = callback();
console.log(observed, typeof observed);
function invoke(callback: () => void): void { return callback(); }
console.log(invoke(callback));
function empty(): void { }
console.log(empty(), invoke(empty));
function bareReturn(): void { return; }
console.log(bareReturn());
function printed(): void { return console.log("printed"); }
console.log(printed());
const undefinedResult = console.log("assigned");
console.log(undefinedResult, typeof undefinedResult);
let effects = 0;
const other: () => void = () => { effects++;return "text"; };
console.log(other(), effects, invoke(other));
console.log(void other(), effects);
const object = { value: callback() };
const { value } = object;
console.log(value, typeof value);
const values: void[] = [callback(), other(), empty()];
console.log(values[0], values[1], values[2]);
let assigned: void;
console.log(assigned);
assigned = callback();
console.log(assigned, typeof assigned);
function pass(value: void): void { return value; }
console.log(pass(callback()), pass(undefined));
const predicate: (value: unknown) => void = Number.isInteger;
console.log(predicate(2), predicate(2.5), typeof predicate(2));
function conditional(flag: boolean): void { if (flag) return callback(); }
console.log(conditional(true), conditional(false));
function forward(): void { return invoke(callback); }
console.log(forward());
console.log((console.log)("parenthesized"));
console.log(console.log(...["spread"]));

const returnsNull: () => void = () => null;
console.log(returnsNull());
const shared = { x: 1 };
const returnsObject: () => void = () => shared;
const objectValue: unknown = returnsObject();
console.log(objectValue === shared);
interface VoidRunner { run: () => void; }
const runner: VoidRunner = { run: () => ({ value: 9 }) };
const resultFromMethod: any = runner.run();
console.log(resultFromMethod.value);
console.log(exported(), moduleResult, typeof moduleResult);

function procedureLoop(count: number, seed: number): number {
    const cell = { value: seed };
    for (let i = 0; i < count; i++) incrementCell(cell);
    return cell.value;
}
function incrementCell(cell: { value: number }): void { cell.value += 1; }
console.log(procedureLoop(8, 7));
