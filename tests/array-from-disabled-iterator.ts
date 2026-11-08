async function run(): Promise<void> {
    const fromAsync = (Array as any).fromAsync;
    for (const method of [null, undefined]) {
        const values = [1,2];
        (values as any)[Symbol.iterator] = method;
        const mapped = Array.from(values, (value, index) => { if(index === 0) values.push(3); return value; });
        console.log('sync length snapshot', mapped.join(','), values.length);
        const asyncValues = [4,5];
        (asyncValues as any)[Symbol.iterator] = method;
        console.log('async length snapshot', (await fromAsync(asyncValues, (value: number, index: number) => { if(index === 0) asyncValues.push(6); return value; })).join(','));
        class Result { length = 0; constructor(...args: any[]) { console.log('constructor length', args.join(',')); } }
        console.log('sync ctor', (Array.from as any).call(Result, values).length);
        console.log('async ctor', (await fromAsync.call(Result, values)).length);
    }
    let reads = 0;
    const values = [7,8];
    const prototype = { get [Symbol.iterator]() { reads++; return null; } };
    Object.setPrototypeOf(prototype, Array.prototype);
    Object.setPrototypeOf(values, prototype);
    console.log('inherited', Array.from(values).join(','), reads);
    reads = 0;
    console.log('inherited async', (await fromAsync(values)).join(','), reads);
    const boxed = new String('abc');
    (boxed as any)[Symbol.iterator] = function*() { yield 'custom'; };
    console.log('boxed', Array.from(boxed).join(','), (await fromAsync(boxed)).join(','));
}
run();
