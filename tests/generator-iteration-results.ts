function* values(): Generator<number, void, unknown> { yield 1; yield 2; yield 3; }
console.log('sum', [...values()].join(','));
const original = values().next;
const replaced = values();
let calls = 0;
replaced.next = function (...args: [] | [unknown]) { calls++; return original.apply(this, args); };
console.log('override', [...replaced].join(','), calls);
const cached = values();
let first = true;
const source = { [Symbol.iterator]() { return cached; } };
cached.next = function () {
    if (first) { first = false; cached.next = () => ({value: undefined, done: true}); }
    return original.call(this);
};
console.log('captured', [...source].join(','));
let reads = 0, steps = 0, closes = 0;
const delegated = {
    [Symbol.iterator]() { return this; },
    next(): IteratorResult<number> {
        steps++;
        return { get value() { reads++; return steps * 10; }, done: steps > 3 };
    },
    return(): IteratorResult<number> { closes++; return {value: 0, done: true}; }
};
function* forwarding() { try { yield* delegated; } finally { console.log('finally'); } }
const [, second] = forwarding();
console.log('elision', second, reads, steps, closes);
function* sent(): Generator<number, void, unknown> {
    const input = yield 1;
    console.log('sent', input === undefined);
    yield 2;
}
console.log([...sent()].join(','));
const distinct = values();
const a = distinct.next(), b = distinct.next();
a.value = 40;
console.log('results', a !== b, a.value, b.value, Object.keys(a).join(','));
function* throwing() { try { yield 5; throw new Error('expected'); } finally { console.log('throw-cleanup'); } }
try { console.log([...throwing()]); } catch (error) { console.log((error as Error).message); }
function* closing() { try { yield 7; yield 8; } finally { yield 9; console.log('closed'); } }
const closingIterator = closing();
for (const item of closingIterator) { console.log('break', item); break; }
console.log('after-break', closingIterator.next().done);


const savedNext = original;
const changedPrototype = values();
const generatorPrototype = Object.getPrototypeOf(Object.getPrototypeOf(changedPrototype));
const prototypeNext = generatorPrototype.next;
try {
    generatorPrototype.next = function () { return {value: 60, done: true}; };
    console.log('prototype', [...changedPrototype].join(','));
    const borrowed = values();
    borrowed.next = savedNext;
    console.log('borrowed', [...borrowed].join(','));
} finally { generatorPrototype.next = prototypeNext; }
let doneReads = 0, valueReads = 0;
const finishedDelegate = {
    [Symbol.iterator]() { return this; },
    next() { return { get done() { doneReads++; return true; }, get value() { valueReads++; return 42; } }; }
};
function* completion() { const result = yield* finishedDelegate; yield result; }
console.log('delegate-return', [...completion()].join(','), doneReads, valueReads);
let reentry: Generator<number, void, unknown>;
const reentrantDelegate = {
    [Symbol.iterator]() { return this; },
    next(): IteratorResult<number> {
        return { done: false, get value() {
            try { reentry.next(); } catch (error) { console.log('reentrant', error instanceof TypeError); }
            return 11;
        } };
    },
    return(): IteratorResult<number> { return {value: undefined, done: true}; }
};
function* reentrantValues() { yield* reentrantDelegate; }
reentry = reentrantValues();
for (const value of reentry) { console.log('reentrant-value', value); break; }
const exhausted = values();
console.log('exhausted-first', [...exhausted].length, 'exhausted-second', [...exhausted].length);
function* nullishValues(): Generator<unknown, void, unknown> { yield null; yield undefined; yield -0; yield NaN; yield false; }
for (const value of nullishValues()) console.log('value', typeof value, value === null, value === undefined, Object.is(value, -0), Object.is(value, NaN));
