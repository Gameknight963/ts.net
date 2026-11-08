/// <reference lib="esnext.iterator" />
export {};
function source(fail: boolean) {
    let index = 0;
    return Iterator.from({
        next() {
            const current = index++;
            console.log('next', current);
            return {
                get done() {console.log('done', current); return current >= 3;},
                get value() {
                    console.log('value', current);
                    if (fail && current < 2) throw new Error('discarded value');
                    return current;
                }
            };
        },
        return() {console.log('return'); return {done: true as const, value: undefined};}
    });
}
for (const count of [0, 1, 2, 5, Infinity]) {
    console.log('count', count);
    console.log('values', source(false).drop(count).toArray().join(','));
}
try {console.log('throwing', source(true).drop(2).toArray().join(','));}
catch (error) {console.log('error', (error as Error).message);}
