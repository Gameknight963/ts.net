function make(seed: number) {
 let value=seed;
 const increment=(delta: number): number => value+=delta;
 const read=(): number => value;
 return {increment,read};
}
const a=make(3), b=make(20);
console.log(a.increment(2),a.read(),a.increment(3),b.increment(1),a.read(),b.read());
function outer(seed: number) { return (n: number) => () => seed+n; }
const nested=outer(4)(5);console.log(nested());
function loop() {
 const functions: (() => number)[]=[];
 for(let i=0;i<3;i++) functions.push(()=>i);
 console.log(functions[0](),functions[1](),functions[2]());
 const values: (() => number)[]=[];
 for(const value of [4,5,6]) values.push(()=>value);
 console.log(values[0](),values[1](),values[2]());
 const shared: (()=>number)[]=[];
 for(var index=0;index<3;index++) shared.push(()=>index);
 console.log(shared[0](),shared[1](),shared[2]());
}
loop();

function mutateParameter(value: number) {
    const read = () => value;
    const write = (next: number) => value = next;
    value += 2;
    return {read, write};
}
const parameter = mutateParameter(3);
console.log(parameter.read(), parameter.write(9), parameter.read());
function references() {
    const item = {value: 2};
    const read = () => item.value;
    const write = (next: number) => item.value = next;
    return {read, write};
}
const reference = references();
console.log(reference.read(), reference.write(7), reference.read());
function repeatedBlocks() {
    const readers: (() => number)[] = [];
    let i = 0;
    while (i < 3) {
        const value = i + 10;
        readers.push(() => value);
        i++;
    }
    return readers;
}
const blocks = repeatedBlocks();
console.log(blocks[0](), blocks[1](), blocks[2]());
function later() {
    const read = () => value;
    const value = 42;
    return read;
}
console.log(later()());
function hoisted() {
    const read = () => typeof value;
    const before = read();
    var value: any = 3;
    return before + ":" + read();
}
console.log(hoisted());
function defaults(seed: number, value = () => seed) {
    seed += 1;
    return value;
}
console.log(defaults(4)(), defaults(4, () => 9)());
function destructured({value}: {value: number}) { return () => value; }
console.log(destructured({value: 6})());
function bindings() {
    const [first, second] = [3, 4];
    const {value} = {value: 5};
    return () => first + second + value;
}
console.log(bindings()());
function recursive(factor: number) {
    return function fact(n: number): number { return n <= 1 ? factor : n * fact(n - 1); };
}
console.log(recursive(2)(5));
function continuation() {
    const readers: (() => number)[] = [];
    for (let i = 0; i < 4; i++) {
        if (i === 1) continue;
        readers.push(() => i);
    }
    console.log(readers[0](), readers[1](), readers[2]());
}
continuation();

const topLevel: (() => number)[] = [];
for (let index = 0; index < 3; index++) topLevel.push(() => index);
console.log(topLevel[0](), topLevel[1](), topLevel[2]());
let blockRead: () => number = () => -1;
{ const value = 12; blockRead = () => value; }
console.log(blockRead());
function loopBindings() {
    const readers: (() => number)[] = [];
    for (const [first, second] of [[1, 2], [3, 4]]) readers.push(() => first + second);
    return readers;
}
const boundReaders = loopBindings();
console.log(boundReaders[0](), boundReaders[1]());

function switches() {
    const readers: (() => number)[] = [];
    for (let i = 0; i < 3; i++) {
        switch (i) {
            default:
                const value = i + 20;
                readers.push(() => value);
        }
    }
    return readers;
}
const switchReaders = switches();
console.log(switchReaders[0](), switchReaders[1](), switchReaders[2]());
function sharedOutsideLoop() {
    let total = 0;
    const writers: (() => number)[] = [];
    for (let i = 0; i < 3; i++) writers.push(() => total += i);
    return writers;
}
const loopWriters = sharedOutsideLoop();
console.log(loopWriters[1](), loopWriters[2](), loopWriters[0]());
function recursiveBinding(seed: number) {
    const fn = (n: number): number => n === 0 ? seed : fn(n - 1) + 1;
    return fn;
}
console.log(recursiveBinding(7)(4));

function parameterVar(value: number) {
    const read = () => value;
    var value: number;
    return read;
}
console.log(parameterVar(8)());
function parameterBindingVar([value]: number[]) {
    const read = () => value;
    var value: number;
    return read;
}
console.log(parameterBindingVar([9])());
