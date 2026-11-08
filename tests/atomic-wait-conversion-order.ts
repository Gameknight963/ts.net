export {};
for (const method of [Atomics.wait, Atomics.waitAsync]) {
    for (const failure of ['none', 'expected', 'timeout']) {
        let effects = '';
        const expected: any = {valueOf() {effects += 'E'; if (failure === 'expected') throw new Error('expected'); return 0;}};
        const timeout: any = {valueOf() {effects += 'T'; if (failure === 'timeout') throw new Error('timeout'); return 0;}};
        try {method(new Int32Array(1), 0, expected, timeout);}
        catch (error) {console.log(method.name, failure, effects, (error as Error).name);}
    }
}
for (const offset of [0, 2 ** 31, 2 ** 40, Number.MAX_SAFE_INTEGER + 1]) {
    const buffer = new ArrayBuffer(4), view = new Int32Array(buffer);
    const index: any = {valueOf() {structuredClone(buffer, {transfer: [buffer]}); return offset;}};
    try {Atomics.load(view, index);} catch (error) {console.log('detached index', offset, (error as Error).name);}
}
