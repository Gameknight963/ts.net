function factory(seed: number) {
    console.log(increment(2));
    function increment(delta: number): number { return seed += delta; }
    function read(): number { return seed; }
    return {increment, read};
}
const first = factory(3);
const second = factory(20);
console.log(first.increment(3), first.read(), second.read());
console.log(first.increment === first.increment, first.increment === second.increment);
function identityFactory() {
    function self(): typeof self { return self; }
    return self;
}
const a = identityFactory();
const b = identityFactory();
console.log(a() === a, b() === b, a === b, a.name, a.length);
function parity() {
    console.log(even(8), odd(8));
    function even(n: number): boolean { return n === 0 || odd(n - 1); }
    function odd(n: number): boolean { return n !== 0 && even(n - 1); }
    return even;
}
console.log(parity()(5));
function blockFactories() {
    const readers: (() => number)[] = [];
    for (let i = 0; i < 3; i++) {
        readers.push(read);
        function read(): number { return i; }
    }
    return readers;
}
const readers = blockFactories();
console.log(readers[0](), readers[1](), readers[2](), readers[0] === readers[1]);
function recursive(seed: number) {
    return count;
    function count(n: number): number { return n <= 0 ? seed : count(n - 1) + 1; }
}
console.log(recursive(7)(4));
function shadow() {
    function read(): number { return 1; }
    const original = read;
    { function read(): number { return 2; } console.log(read()); }
    console.log(read(), original === read);
}
shadow();
function overloads() {
    function value(n: number): number;
    function value(n: string): string;
    function value(n: any): any { return n; }
    console.log(value(3), value("text"));
}
overloads();

function switches(seed: number) {
    switch (seed) {
        case 0:
            return read();
        default:
            function read(): number { return seed + 10; }
            return read;
    }
}
console.log(switches(0), (switches(3) as () => number)());
function switchCapture(seed: number) {
    switch (seed) {
        case 0:
            let value = 7;
            return read;
        default:
            function read(): number { return value; }
            return () => 0;
    }
}
console.log(switchCapture(0)());
