export {};
const owner = {base: 3, ownerMarker: true, method(value: number): number {return this.base + value;}};
function sum(target: {method(value: number): number}, count: number): number {
 let total = 0;
 for (let index = 0; index < count; index++) total += target.method(index);
 return total;
}
console.log("plain", sum(owner, 64));
owner.base = 7;
console.log("changed field", sum(owner, 64));
const original = owner.method;
owner.method = value => value + 20;
console.log("changed method", sum(owner, 64));
owner.method = original;
let methodGets = 0;
const accessor = {base: 11, get method() {methodGets++; return original;}};
console.log("method accessor", sum(accessor, 64), methodGets);
let fieldGets = 0;
const fieldAccessor = {get base() {fieldGets++; return 13;}, method: original};
console.log("field accessor", sum(fieldAccessor, 64), fieldGets);
const wider = {base: 17, extra: "other layout", method: original};
console.log("other layout", sum(wider, 64));
const recursive = {base: 2, calls: 0, method(value: number): number {this.calls++; return this.base + value;}};
console.log("effectful body", sum(recursive, 64), recursive.calls);
let emptyGets = 0;
const empty = {get method() {emptyGets++; return original;}};
console.log("empty", sum(empty, 0), emptyGets);

const first = {base: 2, firstMarker: true, choose(value: number): number {return this.base + value;}};
const second = {base: 5, secondMarker: "second", choose(value: number): number {return this.base * 2 + value;}};
function alternating(): number {
 let total = 0;
 for (let index = 0; index < 64; index++) total += (index & 1 ? first : second).choose(index);
 return total;
}
console.log("alternating", alternating());
function unselected(): number {
 let total = 0;
 for (let index = 0; index < 64; index++) total += (index & 0 ? later : first).choose(index);
 return total;
}
console.log("uninitialized branch", unselected());
const later = {base: 9, laterMarker: 3, choose(value: number): number {return this.base + value;}};

function effects(): number {
 let total = 0, calls = 0;
 function argument(value: number): number {calls++; return value;}
 for (let index = 0; index < 64; index++) total += owner.method(argument(index));
 return total + calls;
}
console.log("argument effects", effects());
const sparse: any = {base: undefined, method: original};
console.log("undefined field", sum(sparse, 64));
const indexed: {[name: string]: any; indexed(value: number): number} = {base: 4, indexed(value: number): number {return this.base + value;}};
console.log("index signature", indexed.indexed(3));
