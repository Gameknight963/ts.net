export {};
const search = RegExp.prototype[Symbol.search] as Function;
for (const initial of [0, -0, NaN, undefined, {valueOf() {throw new Error('must not convert');}}]) {
    for (const mode of ['match', 'null', 'throw', 'invalid']) {
        let state: any = initial, log = '';
        const source = {
            get lastIndex() {log += 'G'; return state;},
            set lastIndex(value: any) {log += 'S'; state = value;},
            exec(text: string): any {
                log += 'E'; state = 7;
                if (mode === 'throw') throw new Error('exec');
                if (mode === 'invalid') return 3;
                return mode === 'null' ? null : {get index() {log += 'I'; return 'custom index';}};
            }
        };
        try {console.log('result', mode, search.call(source, 'abc'), Object.is(state, initial), log);}
        catch (error) {console.log('error', mode, (error as Error).name, Object.is(state, initial), state === 7, log);}
    }
}
for (const lastIndex of [0, 3]) {
    const regex = /a/;
    regex.lastIndex = lastIndex;
    Object.defineProperty(regex, 'lastIndex', {writable: false});
    try {console.log('readonly', lastIndex, search.call(regex, 'a'), regex.lastIndex);}
    catch (error) {console.log('readonly error', lastIndex, (error as Error).name, regex.lastIndex);}
}
