function maker(seed: number) {
    return function self() { return {self, seed}; };
}
const first = maker(3), second = maker(8);
console.log(first().self === first, second().self === second, first === second, first().seed, second().seed);
console.log(first.name, first.length, typeof first);
const standalone = function only() { return only; };
console.log(standalone() === standalone, standalone().name);
function nested(seed: number) {
    return function self(delta: number) { return () => ({self, result: seed + delta}); };
}
const callback = nested(7);
const result = callback(4)();
console.log(result.self === callback, result.result);
let changed: any = function original() { return original; };
const saved = changed;
changed = () => 0;
console.log(saved() === saved, saved() === changed);
const defaulted = function identity(value: any = identity) { return value; };
console.log(defaulted() === defaulted, defaulted(3));
const shadowed = function hidden(hidden: number) { return hidden + 1; };
console.log(shadowed(3));
const recursive = function fact(n: number): number { return n <= 1 ? 1 : n * fact(n - 1); };
console.log(recursive(5));
const recursiveValue = function count(n: number): any { return n <= 0 ? count : count(n - 1); };
console.log(recursiveValue(3) === recursiveValue);
function local(count: number, seed: number): number {
    const value = function self() { return self; };
    let total = seed;
    for (let i = 0; i < count; i++) if (value() === value) total++;
    return total;
}
console.log(local(8, 3));
