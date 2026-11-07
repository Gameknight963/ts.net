function* loops() {
    const saved: (() => number)[] = [];
    outer: for (let i = 0; i < 4; i++) {
        for (let j = 0; j < 3; j++) {
            try {
                if (j === 1) continue outer;
                saved.push(() => i);
                yield i * 10 + j;
                if (i === 2) break outer;
            } finally { console.log('loop-finally', i, j); }
        }
    }
    yield saved.map(f => f()).join(',');
    let count = 0;
    while (count < 2) yield ++count;
    do { yield count--; } while (count > 0);
    switch (count) {
        case 0: yield 'zero';
        case 1: yield 'fallthrough'; break;
        default: yield 'wrong';
    }
}
console.log([...loops()].join('|'));
function* expressions(): Generator<unknown, number, number> {
    const values = [1, yield 2, 3];
    const object = { prefix: 4, ['key' + (yield 5)]: 6 };
    yield values.join(',');
    yield Object.keys(object).join(',');
    const sum = 10 + (yield 7) * 2;
    const picked = (yield 8) ? yield 9 : yield 10;
    yield sum + picked;
    return 99;
}
const expression = expressions();
for (let i = 0; i < 9; i++) console.log(JSON.stringify(expression.next(i)));
function* innerLoop() { try { yield 1; yield 2; } finally { console.log('inner-closed'); } }
function* outerLoop() { for (const value of innerLoop()) yield value * 10; }
const loop = outerLoop();
console.log(JSON.stringify(loop.next()));
console.log(JSON.stringify(loop.return()));
function* nested(): Generator<number, string, unknown> {
    try {
        try { yield 1; }
        finally { console.log('inner-finally'); yield 2; }
    } finally { console.log('outer-finally'); yield 3; }
    return 'normal';
}
const returned = nested();
console.log(JSON.stringify(returned.next()));
console.log(JSON.stringify(returned.return('early')));
console.log(JSON.stringify(returned.next()));
console.log(JSON.stringify(returned.next()));
const thrown = nested();
console.log(JSON.stringify(thrown.next()));
console.log(JSON.stringify(thrown.throw('failure')));
console.log(JSON.stringify(thrown.next()));
try { thrown.next(); } catch (error) { console.log('throw-done', error); }
function* returnsInFinally(): Generator<number, string, unknown> {
    try { yield 1; } finally { yield 2; return 'overridden'; }
}
const overridden = returnsInFinally(); overridden.next();
console.log(JSON.stringify(overridden.return('original')));
console.log(JSON.stringify(overridden.next()));
const custom = {
    step: 0,
    [Symbol.iterator]() { return this; },
    next(value?: unknown) { console.log('delegate-next', value); return {done: this.step++ > 0, value: 10}; },
    return(value?: unknown) { console.log('delegate-return', value); return {done: false, value: 20}; },
    throw(value?: unknown) { console.log('delegate-throw', value); return {done: true, value: 30}; }
};
function* forwarding(): Generator<number, number, unknown> { const result = yield* custom; yield Number(result); return 40; }
const forwarded = forwarding();
console.log(JSON.stringify(forwarded.next()));
console.log(JSON.stringify(forwarded.return(50)));
console.log(JSON.stringify(forwarded.throw('recover')));
console.log(JSON.stringify(forwarded.next()));
function* receivers(this: { value: number }, n: number) { yield this.value; yield arguments[0]; yield n; }
console.log([...receivers.call({value: 12}, 34)].join(','));
class Parent { value() { return 42; } }
class Child extends Parent { *items() { yield super.value(); yield () => this.value(); } }
const child = new Child().items();
console.log(child.next().value, (child.next().value as () => number)());
let reentrant: Generator;
function* reenter() { try { reentrant.next(); } catch (error) { yield error instanceof TypeError; } }
reentrant = reenter();
console.log(JSON.stringify(reentrant.next()));
console.log(JSON.stringify(reentrant.next()));
function* empty() { return 7; }
console.log(JSON.stringify(empty().next()));
console.log(Object.getPrototypeOf(empty()) === empty.prototype);

function* defining(): Generator<string, number, string> {
    class Defined { [yield 'key']() { return 123; } }
    return (new Defined() as any).method();
}
const definition = defining();
console.log(JSON.stringify(definition.next()));
console.log(JSON.stringify(definition.next('method')));

function* scopes(): Generator<() => number, void, unknown> {
    const read = () => later;
    yield read;
    let later = 7;
    yield read;
}
const scoped = scopes();
const readLater = scoped.next().value as () => number;
try { readLater(); } catch (error) { console.log(error instanceof ReferenceError); }
scoped.next();
console.log(readLater());
function* destructuring(): Generator<any, number, any> {
    const [a, b = yield 1] = yield [2];
    return a + b;
}
const destructured = destructuring();
console.log(JSON.stringify(destructured.next()));
console.log(JSON.stringify(destructured.next([4])));
console.log(JSON.stringify(destructured.next(8)));
async function consume() { let sum = 0; for await (const value of innerLoop()) sum += value; return sum; }
consume().then(sum => console.log('async-consumed', sum));
