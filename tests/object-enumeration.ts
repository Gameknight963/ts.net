function valuesText(values: any[]): string { let text="";for(const value of values) text += typeof value + ":" + value + "|";return text; }
function entriesText(entries: [string, any][]): string { let text="";for(const [key,value] of entries) text += key+"="+value+"|";return text; }
const source = {z: 3, "10": 10, "2": 2, a: "text", missing: undefined};
console.log(valuesText(Object.values(source)));
console.log(entriesText(Object.entries(source)));
const values = Object.values;
const entries = Object["entries"];
console.log(values===Object["values"], entries===Object.entries, values.name, entries.name, values.length, entries.length);
console.log(valuesText(values(source)), entriesText(entries(source)));
const args: [any] = [source];
console.log(valuesText(Object.values(...args)), entriesText(Object.entries(...args)));
const array = [1,,undefined,4];
console.log(valuesText(Object.values(array)), entriesText(Object.entries(array)));
console.log(valuesText(Object.values(1)), entriesText(Object.entries(false)));
console.log(valuesText(Object.values("ab")), entriesText(Object.entries("ab")));
let unicode="";for(const char of Object.values("A\uD83D\uDE00")) unicode += char;
console.log(unicode);
const numerical = {x: NaN, y: -0, z: undefined};
const numericValues = Object.values(numerical);
console.log(Number.isNaN(numericValues[0]), 1/(numericValues[1] as number), numericValues[2]===undefined);
const object = {nested: {x:1}};
const copies = Object.values(object);
console.log(copies[0]===object.nested);
copies[0].x=2;
console.log(object.nested.x);
const first = Object.entries(object);
const second = Object.entries(object);
console.log(first!==second, first[0]!==second[0], first[0][1]===object.nested);
function named(): void {}
const fn: any = named;
fn.extra=12;
console.log(valuesText(Object.values(fn)), entriesText(Object.entries(fn)));
const dynamic: any = {};
for(let i=0;i<10;i++) dynamic["key"+i]=i;
console.log(valuesText(Object.values(dynamic)), entriesText(Object.entries(dynamic)));
function shadowed(): string { const Object={values:()=>[99],entries:()=>[["local",7]]};return valuesText(Object.values())+Object.entries()[0][0]+":"+Object.entries()[0][1]; }
console.log(shadowed());
let calls=0;
function mutate(): number { calls++;(source as any).a="changed";return calls; }
const loose: any = Object.values;
console.log(valuesText(loose(source,mutate())), calls);
const optional: {x?:number} = {};
console.log(valuesText(Object.values(optional)), entriesText(Object.entries(optional)));
optional.x=undefined;
console.log(valuesText(Object.values(optional)), entriesText(Object.entries(optional)));

function ordered(flag:boolean): {a:number;b:number} { if(flag) return {b:2,a:1};return {a:1,b:2}; }
console.log(valuesText(Object.values(ordered(true))), entriesText(Object.entries(ordered(true))));
console.log(valuesText(Object.values(ordered(false))), entriesText(Object.entries(ordered(false))));
function valueCount(value:any):number { return Object.values(value).length; }
function entryCount(value:any):number { return Object.entries(value).length; }
console.log(valueCount(source),entryCount(source));
const writableSource:any={x:1,y:2};
const writable=values(writableSource);
writable[0]="changed";
console.log(writable[0],writableSource.x);
const integerOrder={"10":10,"2":2,z:3,"0":0,"4294967295":4,"4294967294":5};
console.log(valuesText(Object.values(integerOrder)),entriesText(Object.entries(integerOrder)));

console.log(valuesText((Object).values(source)),entriesText((Object)["entries"](source)));
