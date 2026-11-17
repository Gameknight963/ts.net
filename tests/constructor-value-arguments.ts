export {};
const Text: any = function(this: any, value: string): void {this.value = value;};
const Flag: any = function(this: any, value: boolean): void {this.value = value;};
const Value: any = function(this: any, value: any): void {this.value = value;};
const Unknown: any = function(this: any, value: unknown): void {this.value = value;};
const Shape: any = function(this: any, value: {name: string}): void {this.value = value.name;};
const List: any = function(this: any, value: number[]): void {this.value = value[0];};
const NumberValue: any = function(this: any, value: number): void {this.value = value;};
console.log("text", new Text("hello").value);
class Label {constructor(public value: string) {}}
const label = new Label("class");
console.log("class", label.value, label instanceof Label);
console.log("flags", new Flag(true).value, new Flag(false).value);
const object = {name: "object"};
for (const value of [undefined, null, true, 3, "text", object]) {
 console.log("any", new Value(value).value === value, new Unknown(value).value === value);
}
console.log("objects", new Shape(object).value, new List([7]).value);
function dynamic(value: any): any {return new NumberValue(value);}
console.log("dynamic number", dynamic(11).value);
function capture(prefix: string): any {return function(this: any, value: string): void {this.value = prefix + value; this.target = new.target;};}
const Captured = capture("prefix:");
const captured = new Captured("value");
console.log("capture", captured.value, captured.target === Captured);
const Returned: any = function(this: any, value: string): any {this.value = value; return value === "replace" ? {value: "replacement"} : undefined;};
console.log("returned", new Returned("replace").value, new Returned("keep").value);
const VoidReplaces: any = function(this: any, value: string): void {this.value = "original"; return {value} as any;};
console.log("void replacement", new VoidReplaces("returned").value);

const receiver: any = {};
Text.call(receiver, "called");
console.log("call", receiver.value);
const Bound: any = Text.bind(null);
console.log("bound", new Bound("bound").value);
const UsesArguments: any = function(this: any, value: string): void {this.value = arguments[0];};
console.log("arguments", new UsesArguments("argument").value);
const Default: any = function(this: any, value = "default"): void {this.value = value;};
console.log("default", new Default(undefined).value);
const Arrow: any = (value: string): void => {};
try {new Arrow("arrow");} catch(error) {console.log("arrow", (error as Error).name);}

export function kernel(count: number, offset: number): number {
 const Constructor: any = function(this: any, value: string): void {this.value = value;};
 let total = offset;
 for(let i=0;i<count;i++){const item = new Constructor("x") as {value: string};total += item.value.length;}
 return total;
}
export function work(): number {return kernel(100000, 0);}
console.log("sum", work());
