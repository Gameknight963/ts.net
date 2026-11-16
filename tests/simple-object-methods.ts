export {};
const counter = {base: 3, marker: "counter", read(value: number): number {return this.base + value;}};
function invoke(target: {read(value: number): number}, value: number): number {return target.read(value);}
console.log("initial", invoke(counter, 4));
counter.base = 7;
console.log("updated", invoke(counter, 4));
const original = counter.read;
const alternative = {base: 11, read: original};
console.log("borrowed receiver", invoke(alternative, 4));
counter.read = value => value + 20;
console.log("replacement", invoke(counter, 4));
counter.read = original;
let gets = 0;
Object.defineProperty(counter, "base", {configurable: true, get() {gets++; return 13;}});
console.log("field accessor", invoke(counter, 4), gets);
Object.defineProperty(counter, "base", {configurable: true, writable: true, value: undefined});
console.log("undefined field", invoke(counter, 4));
Object.defineProperty(counter, "base", {configurable: true, writable: true, value: 17});
let order = "";
Object.defineProperty(counter, "read", {configurable: true, get() {order += "g"; return original;}});
function argument(): number {order += "a"; Object.defineProperty(counter, "read", {configurable: true, writable: true, value: (value: number) => value + 40}); return 2;}
console.log("captured method", counter.read(argument()), order);
console.log("after replacement", invoke(counter, 2));
Object.defineProperty(counter, "read", {configurable: true, writable: true, value: original});
function replace(): number {counter.read = value => value + 50; return 2;}
console.log("direct captured method", counter.read(replace()), invoke(counter, 2));
const calculated = {factor: 2, offset: 1, marker: false, calculate(value: number): number {return this.factor * value + this.offset;}};
function calculate(target: {calculate(value: number): number}, value: number): number {return target.calculate(value);}
console.log("arithmetic", calculate(calculated, 5));
Object.defineProperty(calculated, "offset", {get() {calculated.factor = 9; return 3;}, configurable: true});
console.log("field order", calculate(calculated, 5), calculated.factor);
const constant = {tag: 1, constant(value: number): number {return 7;}};
console.log("constant", constant.constant(4));
const identity = {tag: 2, identity(value: number): number {return value;}};
console.log("identity", Object.is(identity.identity(-0), -0));
function factory(offset: number) {return {base: 3, closure(value: number): number {return this.base + value + offset;}};}
const captured = factory(11);
console.log("closure fallback", captured.closure(5));
const usesArguments = {base: 2, check(value: number): number {return this.base + value + arguments.length;}};
console.log("arguments fallback", usesArguments.check(4));
const nested = {base: 1, inner(value: number): number {return (() => this.base + value)();}};
console.log("nested fallback", nested.inner(4));

const steady = {seed: 2, steadyMarker: true, evaluate(value: number): number {return this.seed * 2 + value;}, readSeed(value: number): number {return this.seed;}};
function evaluate(target: {evaluate(value: number): number}, value: number): number {return target.evaluate(value);}
console.log("guarded body", evaluate(steady, 3));
steady.seed = 5;
console.log("guarded field update", evaluate(steady, 3), steady.readSeed(0));
const evaluateOriginal = steady.evaluate;
const otherLayout = {seed: 7, extra: "borrowed", evaluate: evaluateOriginal};
console.log("guarded receiver fallback", evaluate(otherLayout, 3));
steady.evaluate = value => value - 1;
console.log("guarded function fallback", evaluate(steady, 3));
steady.evaluate = evaluateOriginal;
function replaceEvaluate(): number {steady.evaluate = value => value + 20; return 3;}
console.log("guarded captured function", steady.evaluate(replaceEvaluate()), evaluate(steady, 3));
