export {};
for (const method of ['union', 'intersection', 'difference', 'symmetricDifference']) {
    const source = new Set([1, 2, 3]);
    let log = '';
    const other = {
        get size() {log += 'S'; source.add(4); return 0;},
        get has() {log += 'H'; source.add(5); return () => false;},
        get keys() {log += 'K'; source.add(6); return function() {
            log += 'C'; source.delete(1); source.add(7);
            let index = 0;
            return {get next() {
                log += 'N'; source.delete(2); source.add(8);
                return function() {
                    log += 'X'; source.delete(3); source.add(9);
                    return index++ === 0 ? {done: false, value: 4} : {done: true};
                };
            }};
        };}
    };
    const result = (Set.prototype as any)[method].call(source, other);
    console.log(method, Array.from(result).join(','), Array.from(source).join(','), log);
}
