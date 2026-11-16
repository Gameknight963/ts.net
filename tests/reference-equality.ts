export {};
const object = {value: 1};
const alias = object;
const other = {value: 1};
const array = [1, 2];
const functionValue = function self() {return self;};
console.log("identity", object === alias, object !== other, array === array, functionValue() === functionValue);
const nan: any = NaN;
const nanAlias = nan;
console.log("nan", nan === nanAlias, nan !== nanAlias);
const first = ["ab", "cd"].join("");
const second = ["a", "b", "c", "d"].join("");
console.log("strings", first === second, first !== second);
let changed: any = {value: 2};
changed = ["ab", "cd"].join("");
console.log("changed string", changed === second);
changed = NaN;
console.log("changed nan", changed === changed);
let asserted: any = {value: 3};
(asserted as any) = NaN;
console.log("asserted assignment", asserted === asserted);
let iterated: any = {value: 4};
for (iterated of [NaN, 1]) console.log("iteration", iterated === iterated);
let destructured: any = {value: 5};
[destructured] = [NaN];
console.log("destructuring", destructured === destructured);
function redeclared(): boolean {var value: any = {}; var value: any = NaN; return value === value;}
console.log("redeclared", redeclared());
const coercible = {valueOf() {return 7;}};
console.log("coercion", (coercible as any) == 7, (coercible as any) === 7, (coercible as any) != 7);
console.log("nullish", undefined === undefined, null === null, (undefined as any) === null);
let order = "";
function left(): any {order += "l"; return object;}
function right(): any {order += "r"; return alias;}
console.log("order", left() === right(), order);
function captured(): void {
 function read(): boolean {return value === undefined;}
 try {console.log(read());} catch (error) {console.log("captured early", (error as Error).name);}
 const value: any = {};
 console.log("captured initialized", read());
}
captured();
const factory = (() => {const value = () => 1; return value;})();
console.log("factory", factory === factory);
const changedFactory = (() => {let value: any = () => 1; value = NaN; return value;})();
console.log("changed factory", changedFactory === changedFactory);

function defaulted(value: any = {}): boolean {return value === value;}
function functionDefault(value: any = () => 1): boolean {return value === value;}
console.log("default parameter", defaulted(), defaulted(NaN), functionDefault(), functionDefault(NaN));
