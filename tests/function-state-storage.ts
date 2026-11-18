export {};

function make(offset: number) {
    return function add(value: number): number {return offset + value;};
}
const add = make(10);
const source = add.toString();
console.log('body', add(2));
console.log('prototype', typeof add.prototype, add(3));
add.prototype = {marker: 42};
console.log('changed', add(4), add.prototype.marker, add.toString() === source);
const bound = add.bind(null, 5);
console.log('bound', bound(), bound.name, bound.length);

function Point(this: {value: number}, value: number): void {this.value = value;}
const Constructor = Point as unknown as {new(value: number): {value: number}; prototype: object};
const first = new Constructor(7);
const prototype = {marker: 'new'};
Constructor.prototype = prototype;
const second = new Constructor(9);
console.log('construct', first.value, second.value, Object.getPrototypeOf(second) === prototype);
console.log('old prototype', Object.getPrototypeOf(first) !== prototype);

function plain(value: number): number {return value * 2;}
console.log('numeric', plain.call(null, 3), plain.apply(null, [4]));
Object.defineProperty(plain, 'prototype', {value: {changed: true}});
console.log('numeric after prototype', plain.call(null, 5), plain.apply(null, [6]));
