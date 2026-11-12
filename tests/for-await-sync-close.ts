async function run(mode: number): Promise<void> {
    let closed = 0;
    const source: Iterable<any> = {[Symbol.iterator]() {
        let next = 0;
        return {
            next() {return next++ === 0 ? {done: false, value: mode === 0 ? Promise.reject('value') : 7} : {done: true, value: undefined};},
            return() {
                closed++;
                console.log('return', mode);
                return {done: false, value: {then(resolve: any, reject: any): void {
                    console.log('close value', mode);
                    if (mode === 2 || mode === 4) reject('close'); else resolve(9);
                }}};
            }
        };
    }};
    try {
        for await (const value of source) {
            console.log('value', mode, value);
            if (mode >= 3) throw 'body';
            break;
        }
        console.log('finished', mode);
    } catch (error) {console.log('caught', mode, error);}
    console.log('closed', mode, closed);
}
async function main(): Promise<void> {for (let mode = 0; mode < 5; mode++) await run(mode);}
void main();
