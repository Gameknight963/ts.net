export {};
for (const method of ['sort', 'toSorted']) {
    for (const failLength of [false, true]) for (const comparator of [undefined, 1]) {
        let log = '';
        const source = {get length() {log += 'L'; if (failLength) throw new Error('length'); return 2;}, get 0() {log += 'V'; return 3;}, set 0(value: number) {log += 'W';}};
        try {const result = (Array.prototype as any)[method].call(source, comparator); console.log(method, failLength, typeof comparator, result.length, log);}
        catch (error) {console.log('error', method, failLength, typeof comparator, (error as Error).name, (error as Error).message === 'length', log);}
    }
}
for (const method of ['sort', 'toSorted']) {
    let comparisons = 0, conversions = 0;
    const source = [3, 1, 2];
    const result = (Array.prototype as any)[method].call(source, (left: number, right: number) => {
        comparisons++;
        return {valueOf() {conversions++; source[0] = 99; return left - right;}};
    });
    console.log('conversion', method, result.join(','), conversions === comparisons, source.join(','));
    const sparse = [3, , 1];
    const sorted = (Array.prototype as any)[method].call(sparse);
    console.log('holes', method, sorted.join(','), Object.keys(sorted).join(','), sorted.length);
}
