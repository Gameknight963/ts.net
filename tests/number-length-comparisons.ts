export {};
function compare(left: number, value: any): void {
    console.log(left < value.length, left > value.length, left <= value.length, left >= value.length);
}
for (const value of [[], [1, 2, 3], [1, 'text', true], 'abc', new Uint8Array(3), {length: '3'}, {length: 3n}, {length: undefined}]) {
    compare(2, value); compare(3, value); compare(NaN, value);
}
compare(undefined as unknown as number, []);
let effects = '';
function left(): number {effects += 'L'; return 2;}
function receiver(): any {effects += 'R'; return {get length() {effects += 'G'; return {valueOf() {effects += 'V'; return 3;}};}};}
console.log('order', left() < receiver().length, effects);
let count = 0;
const changing: any = {get length() {return ++count;}};
compare(2, changing);
console.log('reads', count);
const reads: string[] = [];
const proxy: any = new Proxy([1, 2, 3], {get(target, key, receiver) {reads.push(String(key)); return Reflect.get(target, key, receiver);}});
compare(2, proxy);
console.log('proxy reads', reads.join(','));
const values: any = [1, 2, 3];
let seen = '';
for (let i = 0; i < values.length; i++) {seen += values[i]; if (i === 0) values.push(4);}
console.log('growth', seen);
for (const value of [null, undefined, {length: Symbol()}]) {
    try {console.log(2 < (value as any).length);} catch (error) {console.log('error', (error as Error).name);}
}
