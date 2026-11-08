export {};
for (const method of [Symbol.match, Symbol.replace]) {
    let log = '';
    const source = {
        get flags() {log += 'F'; return 'g';},
        get global() {log += 'G'; return true;},
        get unicode() {log += 'U'; return false;},
        get unicodeSets() {log += 'V'; return false;},
        get lastIndex() {log += 'L'; return 0;},
        set lastIndex(value: number) {log += 'S';},
        exec() {log += 'E'; return null;}
    };
    console.log(String(method), (RegExp.prototype as any)[method].call(source, 'abc', 'x'), log);
}
