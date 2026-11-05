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
