export {};
let trace: string[] = [];
function mark(name: string, value: number): number {trace.push(name); return value;}
class Base {
 value = mark("base field", 1);
 target: unknown;
 constructor(value: number) {trace.push("base body"); this.value = value; this.target = new.target;}
}
class Derived extends Base {extra = mark("derived field", 2);}
const base = new Base(mark("argument", 3));
console.log("base", base.value, base.target === Base, trace.join(","));
trace = [];
const derived = new Derived(mark("argument", 5));
console.log("derived", derived.value, derived.extra, derived.target === Derived, trace.join(","));
function factory(offset: number): any {
 class Captured {value = offset; read = () => this.value; constructor(value: number) {this.value += value;}}
 return Captured;
}
const Captured = factory(7);const captured = new Captured(11);
console.log("capture", captured.value, captured.read());
class Arguments {
 value = 0;
 constructor(value: number) {this.value = arguments.length + arguments[0]; arguments[0] = 99; console.log("arguments", value, arguments[0]);}
}
console.log("args result", new Arguments(13).value);
class Default {value = 1; constructor(value = mark("default", 17)) {this.value = value;}}
trace = [];console.log("default", new Default(undefined as unknown as number).value, trace.join(","));
class Rest {value = 1; constructor(...values: number[]) {this.value = values[0];}}
console.log("rest", new Rest(19).value);
class Replacement {value = 1;constructor(value: number) {return {value: value + 1} as any;}}
console.log("replacement", new Replacement(23).value);
class Primitive {value = 1;constructor(value: number) {return value as any;}}
console.log("primitive", new Primitive(29).value);
class Unknown {value: unknown = 1;constructor(value: unknown) {this.value = value;}}
console.log("unknown", new Unknown(31).value);
class Text {value = "";constructor(value: string) {this.value = value;}}
console.log("text", new Text("text").value);
class Empty {value = 37;}
console.log("implicit", new (Empty as any)(41).value);
class Throws {value = mark("field", 43);constructor(value: number) {throw new Error(String(value));}}
trace = [];try {new Throws(47);} catch(error) {console.log("throw", (error as Error).message, trace.join(","));}

export function kernel(count: number, offset: number): number {
 class Point {value = 0; constructor(value: number) {this.value = value;}}
 const points: Point[] = [];
 for(let i=0;i<count;i++) points.push(new Point(i));
 let sum=offset;
 for(let i=0;i<points.length;i++) sum+=points[i].value;
 return sum;
}
console.log("retained", kernel(10, 0));
