function units(value: string): string {
    const result: number[] = [];
    for (let index = 0; index < value.length; index++) result.push(value.charCodeAt(index));
    return result.join(',');
}
for (let code = 0; code < 128; code++) {
    const value = String.fromCharCode(code);
    const uri = encodeURI(value), component = encodeURIComponent(value);
    console.log('ascii', code, uri, component, units(decodeURI(uri)), units(decodeURIComponent(component)));
}
for (const value of ['hello world', 'a/b?x=y&z=1#tag', '\u00E9', '\u4E16\u754C', '\uD83D\uDE00', '\uFFFF', '\uDBFF\uDFFF']) {
    console.log('unicode', encodeURI(value), encodeURIComponent(value), units(decodeURIComponent(encodeURIComponent(value))));
}
for (const value of ['%2f%2F%3f%23%25', '%21%27%28%29', 'a+b', '%00', '\uD800']) console.log('decode', units(decodeURI(value)), units(decodeURIComponent(value)));
for (const value of ['%', '%0', '%GG', '%80', '%C0%AF', '%C1%BF', '%E0%80%80', '%ED%A0%80', '%F0%80%80%80', '%F4%90%80%80', '%F5%80%80%80', '%E2x%82%AC', '%E2%82', '%E2%28%A1']) {
    for (const method of [decodeURI, decodeURIComponent]) {
        try {method(value);} catch(error) {console.log('invalid decode', value, (error as Error).name, error instanceof URIError);}
    }
}
for (const value of ['\uD800', '\uDC00', 'a\uD800b']) {
    for (const method of [encodeURI, encodeURIComponent]) {
        try {method(value);} catch(error) {console.log('invalid encode', (error as Error).name, error instanceof URIError);}
    }
}
const object = {toString(): string {console.log('convert');return 'a b';}};
console.log('coerced', encodeURIComponent(object as any), decodeURIComponent(object as any));
for (const method of [encodeURI, encodeURIComponent, decodeURI, decodeURIComponent]) {
    console.log('metadata', method.name, method.length, (method as any)(), method(null as any));
    try {method({toString(): never {throw 'conversion failure';}} as any);} catch(error) {console.log('failure', error);}
}
const alias = encodeURIComponent; console.log('alias', alias('a b'));
function shadow(encodeURI: (value: string) => string): string {return encodeURI('local');}
console.log('shadow', shadow(value => value));
