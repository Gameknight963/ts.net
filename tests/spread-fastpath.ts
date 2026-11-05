function kernel(count: number, seed: number): number {
    const source = {x: seed, y: 2};
    let total = 0;
    for (let i = 0; i < count; i++) {
        source.x = i + seed;
        const copy = {...source};
        copy.y = i;
        total += copy.x + copy.y;
    }
    return total;
}
console.log(kernel(1000, 3));
function read(source: {x: number; y: number}): number {
    const copy = {...source};
    copy.y = 7;
    return copy.x + copy.y + source.y;
}
const wider = {x: 2, y: 3, extra: 4};
console.log(read(wider));
function ownExtra(source: {x: number; y: number}): boolean {
    const copy = {...source};
    return Object.hasOwn(copy, "extra");
}
console.log(ownExtra(wider), ownExtra({x: 2, y: 3}));
function ownKnown(source: {x: number; y: number}): boolean {
    const copy = {...source};
    return Object.hasOwn(copy, "x");
}
console.log(ownKnown(wider));
function optional(source: {x?: number}): boolean {
    const copy = {...source};
    return Object.hasOwn(copy, "x");
}
console.log(optional({}), optional({x: undefined}));
function snapshot(): number {
    const source = {x: 2, y: 3};
    const copy = {...source};
    source.x = 8;
    copy.y = 5;
    return copy.x + source.y;
}
console.log(snapshot());
function escaping(source: {x: number; y: number}) { return {...source}; }
const escaped = escaping(wider);
escaped.x = 9;
console.log(escaped.x, wider.x, Object.keys(escaped).length);
const nested = {x: {n: 1}, y: true};
function references(): number {
    const copy = {...nested};
    copy.x.n = 7;
    copy.y = false;
    return nested.x.n;
}
console.log(references(), nested.y);
