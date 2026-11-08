export {};
function prepare(): {array: number[]; seen: string[]} {
    const array = [1, 2, 3];
    delete array[0];
    const seen: string[] = [];
    const prototype = Object.create(Array.prototype);
    Object.defineProperty(prototype, '0', {get() {return 9;}, set(value) {seen.push(String(value));}, configurable: true});
    Object.setPrototypeOf(array, prototype);
    return {array, seen};
}
const fill = prepare();
fill.array.fill(7, 0, 2);
console.log('fill', fill.array.join(','), fill.seen.join(','), Object.hasOwn(fill.array, '0'));
const copy = prepare();
copy.array.copyWithin(0, 1, 3);
console.log('copy', copy.array.join(','), copy.seen.join(','), Object.hasOwn(copy.array, '0'));
const shift = prepare();
console.log('shift', shift.array.shift(), shift.array.join(','), shift.seen.join(','));
const prepend = prepare();
console.log('unshift', prepend.array.unshift(4), prepend.array.join(','), prepend.seen.join(','));
for (const method of ['fill', 'copyWithin']) {
    const array = [1, 2, 3];
    let seen = 0;
    const start = {valueOf() {
        delete array[0];
        const prototype = Object.create(Array.prototype);
        Object.defineProperty(prototype, '0', {get() {return 8;}, set(value: number) {seen = value;}});
        Object.setPrototypeOf(array, prototype);
        return 0;
    }};
    if (method === 'fill') array.fill(5, start as any, 1);
    else array.copyWithin(start as any, 1, 2);
    console.log('conversion', method, seen, array.join(','), Object.hasOwn(array, '0'));
}
for (const method of ['fill', 'copyWithin', 'unshift']) {
    const array = [1, 2, 3];
    delete array[0];
    const prototype = Object.create(Array.prototype);
    Object.defineProperty(prototype, '0', {value: 8, writable: false});
    Object.setPrototypeOf(array, prototype);
    try {
        if (method === 'fill') array.fill(5);
        else if (method === 'copyWithin') array.copyWithin(0, 1);
        else array.unshift(4);
        console.log('unexpected');
    } catch (error) {console.log('readonly', method, (error as Error).name, array.length, array.join(','));}
}
