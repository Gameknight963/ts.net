export {};
async function kernel(count: number, offset: number): Promise<number> {
    let total = 0;
    for (let i = 0; i < count; i++) total += await (i + offset);
    return total;
}
async function captured(): Promise<string> {
    const before: (() => number)[] = [], after: (() => number)[] = [];
    let total = 0;
    for (let i = 0; i < 8; i++) {
        before.push(() => i);
        const value = await (i * 2);
        after.push(() => value);
        total += await value;
    }
    return total + ':' + before.map(read => read()).join(',') + ':' + after.map(read => read()).join(',');
}
let nested: Promise<number> = Promise.resolve(0);
async function reentrant(): Promise<number> {
    let total = 0;
    for (let i = 0; i < 4; i++) {
        const value = Promise.resolve(i);
        Object.defineProperty(value, 'constructor', {get(){nested = kernel(12, 100); return Promise;}, configurable: true});
        total += await value;
    }
    return total;
}
async function protectedRegions(): Promise<number> {
    let total = 0;
    for (let i = 0; i < 8; i++) {
        try {total += await (i % 2 ? Promise.reject(i) : Promise.resolve(i));}
        catch (error) {total += error as number;}
        finally {total += await 1;}
    }
    return total;
}
async function protectedCaptures(offset: number): Promise<string> {
    const reads: (() => number)[] = [];
    for (let i = 0; i < 4; i++) {
        let value = offset + i;
        try {
            const before = value;
            reads.push(() => before);
            value += await i;
            if (i % 2) throw value;
        } catch (error) {
            const caught = error as number;
            reads.push(() => caught);
        } finally {
            const after = await value;
            reads.push(() => after);
        }
        reads.push(() => value);
    }
    return reads.map(read => read()).join(',');
}
async function* values(): AsyncGenerator<number> {
    let total = 0;
    for (let i = 0; i < 5; i++) {total += await i; yield total;}
}
async function main(): Promise<void> {
    const concurrent = await Promise.all([kernel(40, 0), kernel(40, 100), kernel(0, 20), kernel(40, -5)]);
    console.log('concurrent', concurrent.join(','));
    console.log('captured', await captured());
    console.log('reentrant', await reentrant(), await nested);
    console.log('protected', await protectedRegions());
    console.log('protected captures', (await Promise.all([protectedCaptures(0), protectedCaptures(100)])).join('|'));
    const output: number[] = []; for await (const value of values()) output.push(value);
    console.log('generator', output.join(','));
}
main();
