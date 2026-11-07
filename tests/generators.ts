let calls = 0;
function initial() { calls++; return 3; }
function* basic(limit = initial()): Generator<number, string, number> {
    console.log('start', limit);
    const sent: number = yield 1;
    for (let i = 0; i < limit; i++) yield sent + i;
    return 'finished';
}
const first = basic();
console.log('created', calls);
console.log(JSON.stringify(first.next(999)));
console.log(JSON.stringify(first.next(10)));
console.log(JSON.stringify(first.next()));
console.log(JSON.stringify(first.next()));
console.log(JSON.stringify(first.next()));
console.log(JSON.stringify(first.next()));
function* cleanup(): Generator<number, string, unknown> {
    try { yield 1; return 'normal'; }
    finally { yield 2; console.log('cleanup'); }
}
const closing = cleanup();
console.log(JSON.stringify(closing.next()));
console.log(JSON.stringify(closing.return('stopped')));
console.log(JSON.stringify(closing.next()));
function* catching(): Generator<number, string, unknown> {
    try { yield 4; }
    catch (error) { console.log('caught', error); yield 5; }
    finally { console.log('finally'); }
    return 'caught-done';
}
const caught = catching();
console.log(JSON.stringify(caught.next()));
console.log(JSON.stringify(caught.throw('reason')));
console.log(JSON.stringify(caught.next()));
function* inner(): Generator<number, string, unknown> { yield 7; yield 8; return 'inner'; }
function* outer(): Generator<number, string, unknown> {
    yield* [1, 2];
    const result = yield* inner();
    yield result.length;
    return 'outer';
}
const delegated = outer();
for (let i = 0; i < 6; i++) console.log(JSON.stringify(delegated.next()));
console.log([...outer()].join(','));
function* breakable() { try { yield 10; yield 20; } finally { console.log('closed'); } }
for (const item of breakable()) { console.log(item); break; }
const object = { value: 10, *items() { yield this.value; yield this.value + 1; } };
console.log([...object.items()].join(','));
class Numbers { constructor(public value: number) {} *items() { yield this.value; yield this.value + 2; } }
console.log([...new Numbers(20).items()].join(','));
const expression = function* named(n: number): Generator<number, void, unknown> { yield n; if (n > 0) yield* named(n - 1); };
console.log([...expression(2)].join(','));
function factory(n: number) { return function* () { yield n; yield ++n; }; }
const sequence = factory(4);
console.log([...sequence()].join(','), [...sequence()].join(','));
const unopened = breakable();
console.log(JSON.stringify(unopened.return()));
try { unopened.throw('after'); } catch (error) { console.log(error); }

function* infinite() { let n = 0; try { while (true) yield n++; } finally { console.log('infinite-closed'); } }
const lazy = infinite();
console.log(lazy.next().value, lazy.next().value, lazy.next().value);
console.log(JSON.stringify(lazy.return()));

console.log((infinite() as any).take(3).toArray().join(','));

function* characters() { yield* 'a\ud83d\ude00b'; }
console.log([...characters()].join('|'));
