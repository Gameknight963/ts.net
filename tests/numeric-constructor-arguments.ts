export {};
function Point(this: any, value: number): void {this.value = value; this.target = new.target;}
const Constructor: any = Point;
const point = new Constructor(3);
console.log("ordinary", point.value, point.target === Constructor, point instanceof Constructor);
for (const value of [-0, NaN, Infinity, -Infinity]) {const item = new Constructor(value); console.log("numbers", item.value, 1 / item.value);}
Constructor.prototype = {marker: 7};
const changed = new Constructor(5);
console.log("prototype", changed.value, changed.marker, Object.getPrototypeOf(changed) === Constructor.prototype);
Constructor.prototype = null;
const fallback = new Constructor(11);
console.log("fallback", fallback.value, Object.getPrototypeOf(fallback) === Object.prototype);
function factory(offset: number): any {return function(this: any, value: number): void {this.value = value + offset;};}
const Captured = factory(13);
console.log("capture", new Captured(17).value);
const UsesArguments: any = function(this: any, value: number): void {this.value = arguments[0]; this.count = arguments.length;};
const args = new UsesArguments(19);
console.log("arguments", args.value, args.count);
const Default: any = function(this: any, value = 23): void {this.value = value;};
console.log("default", new Default(29).value, new Default(undefined).value);
const Rest: any = function(this: any, ...values: number[]): void {this.value = values[0]; this.count = values.length;};
console.log("rest", new Rest(31).value, new Rest(37).count);
const Replacement: any = function(this: any, value: number): object {return {value: value + 1};};
const replaced = new Replacement(41);
console.log("replacement", replaced.value, replaced instanceof Replacement);
const Primitive: any = function(this: any, value: number): number {this.value = value; return 1;};
console.log("primitive", new Primitive(43).value);
let trace = 0;
const Empty: any = function(value: number): void {trace += value;};
const empty = new Empty(47);
console.log("unused this", trace, Object.getPrototypeOf(empty) === Empty.prototype);
const Arrow: any = (value: number): void => {trace += 100;};
function argument(): number {trace++; return 1;}
try {new Arrow(argument());} catch (error) {console.log("arrow", trace, (error as Error).name);}
try {new ({} as any)(argument());} catch (error) {console.log("invalid", trace, (error as Error).name);}
const Throws: any = function(this: any, value: number): void {throw new Error(String(value));};
try {new Throws(53);} catch (error) {console.log("throws", (error as Error).message);}

const directReceiver: any = {};
Constructor.call(directReceiver, 59);
const appliedReceiver: any = {};
Constructor.apply(appliedReceiver, [61]);
console.log("ordinary calls", directReceiver.value, directReceiver.target === undefined, appliedReceiver.value);
const Bound: any = Constructor.bind(null);
console.log("bound", new Bound(67).value);
const ReturnsUndefined: any = function(this: any, value: number): void {this.value = value; return undefined;};
console.log("undefined return", new ReturnsUndefined(71).value);
const FinallyReplaces: any = function(this: any, value: number): any {try {return undefined;} finally {return {value};}};
console.log("finally replacement", new FinallyReplaces(73).value);
class ClassPoint {constructor(public value: number) {}}
console.log("class", new ClassPoint(79).value);
const NativeNumber: any = Number;
const NativeDate: any = Date;
console.log("builtins", new NativeNumber(83).valueOf(), new NativeDate(0).getTime());
