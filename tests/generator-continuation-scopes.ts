function* sequence(start: number): Generator<() => number, void, unknown> {
    for (let index = start; index < start + 3; index++) {
        yield () => index;
    }
}
const first = sequence(10), second = sequence(20);
const a = first.next().value!, b = second.next().value!, c = first.next().value!, d = second.next().value!;
console.log('interleaved', a(), b(), c(), d(), a !== c, b !== d);
const closures = [...sequence(30)];
console.log('loop-scopes', closures.map(callback => callback()).join(','), closures[0] !== closures[1]);
function* sent(): Generator<number, void, number> {
    let current = 0;
    for (let index = 0; index < 4; index++) { current += yield current; }
}
const left = sent(), right = sent();
console.log('send', left.next().value, right.next().value, left.next(3).value, right.next(7).value, left.next(5).value, right.next(9).value);
function* nested(limit: number): Generator<number, void, unknown> {
    for (let outer = 0; outer < limit; outer++) {
        for (let inner = 0; inner < 2; inner++) yield outer * 10 + inner;
    }
}
console.log('nested', [...nested(3)].join(','), [...nested(2)].join(','));
async function* asyncValues(start: number) {
    for (let index = 0; index < 3; index++) { await Promise.resolve(); yield start + index; }
}
async function check() {
    const one = asyncValues(40), two = asyncValues(50);
    const results = await Promise.all([one.next(), two.next(), one.next(), two.next()]);
    console.log('async', results.map(result => result.value).join(','));
    await one.return(); await two.return();
}
check();
