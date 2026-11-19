function scan(values: any[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) if (typeof values[i] === "number") total++;
    return total;
}
const mixed: any[] = [1, "x", , undefined, null, 0 / 0, false];
console.log(scan(mixed, 7), scan(mixed, 10), scan(mixed, 2.5));
const numeric = [1, 2, 3];
console.log(scan(numeric, 3), scan(numeric, 5));
console.log(scan(undefined as any, 0), scan(undefined as any, -1), scan(undefined as any, 0 / 0));
const sparse: any[] = [];
sparse[100000] = 3;
console.log(scan(sparse, 3));
sparse[0] = 4;
console.log(scan(sparse, 3));
function shrink(values: any[]) { values.length = 1; }
function withCalls(values: any[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (i === 1) shrink(values);
        if (typeof values[i] === "number") total++;
    }
    return total;
}
console.log(withCalls([1, 2, 3], 3));
function withWrites(values: any[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (i === 1) values.length = 1;
        if (typeof values[i] === "number") total++;
    }
    return total;
}
console.log(withWrites([1, 2, 3], 3));
function rebound(values: any[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (i === 1) values = ["new", 4];
        if (typeof values[i] === "number") total++;
    }
    return total;
}
console.log(rebound([1, "old", "old"], 3));
function text(values: string[], count: number) {
    let result = "";
    for (let i = 0; i < count; i++) result += values[i];
    return result;
}
console.log(text(["a", "b", "c"], 3));
function fill(count: number) {
    const values: any[] = [];
    for (let i = 0; i < count; i++) values[i] = i % 2 ? "text" : i;
    return values;
}
const filled = fill(3.5);
console.log(filled.length, filled[0], filled[1], filled[2], filled[3]);

function visit(values: any[], count: number, observe: (value: any, index: number) => void) {
    for (let i = 0; i < count; i++) observe(values[i], i);
}
const direct: any[] = [];
direct.push(1, undefined, null, 'text');
visit(direct, 5, (value, index) => console.log('direct', index, value));
Object.defineProperty(direct, '1', { get() { direct[2] = 9; return 'getter'; }, configurable: true });
visit(direct, 4, (value, index) => console.log('getter', index, value));
const inherited: any[] = [1, , 3];
const inheritedPrototype = Object.create(Array.prototype);
Object.defineProperty(inheritedPrototype, '1', { get() { return 'inherited'; }, configurable: true });
Object.setPrototypeOf(inherited, inheritedPrototype);
visit(inherited, 3, (value, index) => console.log('inherited', index, value));
const changing: any[] = [1, 2, 3];
visit(changing, 4, (value, index) => {
    console.log('changing', index, value);
    if (index === 0) changing.length = 1;
    if (index === 1) changing[2] = 'restored';
});

function dynamicLength(values: any[]) {
    let count = 0;
    for (let i = 0; i < values.length; i++) { console.log('length value', i, values[i]); count++; }
    return count;
}
const resizeDuringRead: any[] = [1, 2, 3];
Object.defineProperty(resizeDuringRead, '0', { get() { resizeDuringRead.length = 1; return 'shrunk'; } });
console.log('live length', dynamicLength(resizeDuringRead));
