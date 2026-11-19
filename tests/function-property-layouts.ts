export {};
function tagged(value: number): number {return value;}
Object.defineProperty(tagged, 'tag', {value: 'metadata', configurable: true});
console.log((tagged as any).tag, tagged(7));
function work(): number {
    let sum = 0;
    for (let i = 0; i < 100000; i++) {
        const point = {x: i, y: i + 1};
        sum += point.x + point.y;
    }
    return sum;
}
console.log(work());
function defineUnion(target: (() => number) | (() => string)): void {
    Object.defineProperty(target, 'tag', {value: 9});
}
const numeric = () => 3;
const textual = () => 'text';
defineUnion(numeric); defineUnion(textual);
console.log(numeric(), textual(), (numeric as any).tag, (textual as any).tag);
function tagGeneric<T extends (value: number) => number>(target: T): void {
    Object.defineProperty(target, 'generic', {value: 'generic metadata'});
}
tagGeneric(tagged);
console.log((tagged as any).generic);
Object.freeze(tagged);
console.log(Object.isFrozen(tagged), Reflect.defineProperty(tagged, 'extra', {value: 1}));
