function counted(count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const point = { x: i, y: 3 };
        const view = point;
        if (i % 2 === 0) view.x++;
        total += point.x + point.y + i % 3;
        if (i === 5) break;
    }
    return total;
}
console.log(counted(0), counted(1), counted(2.5), counted(-3));
console.log(counted(2147483647), counted(2147483648), counted(1 / 0), counted(-1 / 0));
console.log(counted(0 / 0), counted(undefined as unknown as number));
function changingBound(count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) { count--; total += i; }
    return total + count;
}
console.log(changingBound(9));
function changingCounter(count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) { i += 0.5; total += i; }
    return total;
}
console.log(changingCounter(9));
function visibleCounter(count: number) {
    let total = 0;
    for (var i = 0; i < count; i++) total += i;
    return i + total;
}
console.log(visibleCounter(3.5));
function negativeZero() { for (let i = -0; i < 1; i++) return 1 / i; }
console.log(negativeZero());
function labels(count: number) {
    let total = 0;
    outer: for (let i = 0; i < count; i++) {
        const point = { x: i };
        const alias = point;
        for (let j = 0; j < 5; j++) {
            if (j === 1) continue;
            if (i === 3) break outer;
            if (j === 3) continue outer;
            alias.x++;
            total += point.x;
        }
    }
    return total;
}
console.log(labels(10), labels(2147483648));
function preserve(value: number) {
    console.log(value, typeof value, value === undefined);
    return value * 2;
}
console.log(preserve(3), preserve(undefined as unknown as number));
function defaults(first = 3, second = first++) { return first + second; }
console.log(defaults(), defaults(8));
function nestedDynamic(count: number, inner: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const point = { x: i };
        for (let j = 0; j < inner; j++) {
            total += point.x + j;
            if (j === 4) break;
        }
        if (i === 4) break;
    }
    return total;
}
console.log(nestedDynamic(9, 9), nestedDynamic(2147483648, 9), nestedDynamic(9, 2147483648));
function roundedBound(bound: number) {
    let visits = 0;
    for (let i = 0; i < bound; i++) { visits++; if (visits === 4) break; }
    return visits;
}
console.log(roundedBound(0), roundedBound(-0), roundedBound(-1), roundedBound(-1 / 0));
console.log(roundedBound(0.1), roundedBound(1.1), roundedBound(2.9), roundedBound(0 / 0));
console.log(roundedBound(2147483646.5), roundedBound(2147483647), roundedBound(2147483648), roundedBound(1 / 0));

function positiveStart(bound: number) {
    let total = 0;
    for (let i = 3; i < bound; i++) {
        total += i % 7;
        if (i === 6) break;
    }
    return total;
}
console.log(positiveStart(0), positiveStart(3), positiveStart(3.1), positiveStart(5.5));
console.log(positiveStart(2147483647), positiveStart(2147483648), positiveStart(1 / 0));
console.log(positiveStart(0 / 0), positiveStart(undefined as unknown as number));
function nearIntegerLimit() {
    let total = 0;
    for (let i = 2147483645; i < 2147483647; i++) total += i % 3;
    return total;
}
console.log(nearIntegerLimit());
function fractionalStart() {
    let total = 0;
    for (let i = 1.5; i < 4; i++) total += i % 2;
    return total;
}
console.log(fractionalStart());
function emptyArrayLoop(bound: number) {
    const values = undefined as unknown as any[];
    for (let i = 3; i < bound; i++) { values[i] = i; }
    return 0;
}
console.log(emptyArrayLoop(0), emptyArrayLoop(3));
function nonzeroArray() {
    const values: number[] = [1, 2, 3, 4];
    let total = 0;
    for (let i = 1; i < 4; i++) total += values[i];
    return total;
}
console.log(nonzeroArray());
