async function echo(value: number): Promise<number> {
    await Promise.resolve();
    return value;
}
async function defaults(value = 9, second = 2): Promise<number> {
    await Promise.resolve();
    return value + second;
}
async function main(): Promise<void> {
    for (const value of [1, 0, -0, NaN, Infinity]) {
        const result = await echo(value);
        console.log('number', String(result), Object.is(result, value));
    }
    console.log('defaults', await defaults(), await defaults(4), await defaults(4, 5));
    const alias = echo;
    console.log('alias', await alias(17), await echo.call({ignored: true}, 18));
    const holder = {
        offset: 20,
        async add(value: number): Promise<number> {await Promise.resolve(); return this.offset + value;}
    };
    console.log('receiver', await holder.add(3), await holder.add.call({offset: 30}, 4));
    async function count(value: number): Promise<string> {
        const length = arguments.length;
        await Promise.resolve();
        return length + ':' + value;
    }
    console.log('arguments', await count(6), await (count as any)(7, 8));
    async function rest(value: number, ...values: number[]): Promise<string> {
        await Promise.resolve();
        return value + ':' + values.join(',');
    }
    console.log('rest', await rest(1, 2, 3));
    function make(offset: number): (value: number) => Promise<number> {
        return async value => {await Promise.resolve(); return offset + value;};
    }
    const first = make(40), second = make(50);
    console.log('captures', await first(1), await second(2), await first(3));
    async function fail(value: number): Promise<number> {throw new Error('failure:' + value);}
    let pending: Promise<number>;
    try {pending = fail(8); console.log('returned promise');}
    catch {console.log('threw synchronously'); return;}
    try {await pending;} catch (error) {console.log('rejected', (error as Error).message);}
    const jobs: string[] = [];
    async function ordered(value: number): Promise<number> {
        jobs.push('start:' + value);
        await Promise.resolve();
        jobs.push('resume:' + value);
        return value;
    }
    const one = ordered(1), two = ordered(2);
    jobs.push('sync'); await Promise.all([one, two]);
    console.log('order', jobs.join(','));
}
main();
