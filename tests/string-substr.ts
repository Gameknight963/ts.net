export {};
const method: any = String.prototype.substr;
for (const text of ['', 'abcdef', 'a\ud83d\ude00b\ud800']) {
    for (const start of [undefined, null, -Infinity, -9, -2.9, -0, 0, 1.9, 6, 9, NaN, Infinity]) {
        for (const length of [undefined, null, -Infinity, -1, -0, 0, 1.9, 4, 20, NaN, Infinity]) {
            console.log('range', JSON.stringify(text), String(start), String(length), JSON.stringify(method.call(text, start, length)));
        }
    }
}
console.log('direct', 'abcdef'.substr(2, 3), new String('abcdef').substr(-2));
for (const receiver of [12345, true, 12345n, {toString() {return 'custom';}}]) console.log('borrowed', method.call(receiver, 1, 3));
console.log('metadata', method.name, method.length, Object.keys(String.prototype).includes('substr'));
console.log('descriptor', JSON.stringify(Object.getOwnPropertyDescriptor(String.prototype, 'substr'), (key, value) => key === 'value' ? typeof value : value));
let trace = '';
console.log('conversion', method.call({toString() {trace += 'receiver|'; return 'abcdef';}}, {valueOf() {trace += 'start|'; return 2;}}, {valueOf() {trace += 'length|'; return 3;}}), trace);
trace = '';
console.log('empty conversion', method.call('', {valueOf() {trace += 'start|'; return Infinity;}}, {valueOf() {trace += 'length|'; return 3;}}), trace);
for (const receiver of [null, undefined, Symbol('text')]) {
    trace = '';
    try {method.call(receiver, {valueOf() {trace += 'start'; return 0;}});} catch (error) {console.log('receiver error', (error as Error).name, trace);}
}
for (const [start, length] of [[1n, 2], [1, 2n], [Symbol('start'), 2], [1, Symbol('length')]]) {
    try {method.call('text', start, length);} catch (error) {console.log('argument error', (error as Error).name);}
}
