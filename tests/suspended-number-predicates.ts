async function awaitedPredicates(): Promise<void> {
    console.log('spread', Number.isNaN(...(await Promise.resolve([NaN] as [number]))));
    console.log('nan', Number.isNaN(await Promise.resolve(NaN)), Number.isNaN(await Promise.resolve(1)));
    console.log('finite', Number.isFinite(await Promise.resolve(2)), Number.isFinite(await Promise.resolve(Infinity)));
    console.log('integer', Number.isInteger(await Promise.resolve(3)), Number.isInteger(await Promise.resolve(3.5)));
    console.log('safe', Number.isSafeInteger(await Promise.resolve(4)), Number.isSafeInteger(await Promise.resolve(9007199254740992)));
    try { console.log(Number.isInteger(await Promise.reject('expected'))); } catch (error) { console.log('rejection', error); }
}
function* yieldedPredicates(): Generator<number, void, number> {
    console.log('yield-nan', Number.isNaN(yield 1));
    console.log('yield-finite', Number.isFinite(yield 2));
    console.log('yield-integer', Number.isInteger(yield 3));
    console.log('yield-safe', Number.isSafeInteger(yield 4));
}
const generator = yieldedPredicates();
console.log('first', generator.next().value);
console.log('second', generator.next(NaN).value);
console.log('third', generator.next(Infinity).value);
console.log('fourth', generator.next(3.5).value);
console.log('done', generator.next(9007199254740991).done);
async function* asyncPredicates(): AsyncGenerator<boolean, void, number> {
    yield Number.isFinite(await Promise.resolve(3));
    yield Number.isNaN(yield false);
}
async function asyncGeneratorPredicates(): Promise<void> {
    const values = asyncPredicates();
    const first = await values.next(); console.log('async-first', first.value, first.done);
    const second = await values.next(); console.log('async-second', second.value, second.done);
    const third = await values.next(NaN); console.log('async-third', third.value, third.done);
    console.log('async-done', (await values.next()).done);
}
awaitedPredicates(); asyncGeneratorPredicates();
