export {};
type NumericMethod = (...values: number[]) => number;
const holder: {method: NumericMethod, toString(): string, length: number, 0: number, 1: number} = {
    method: String.prototype.charCodeAt as NumericMethod,
    toString() { return '12321'; }, length: 2, 0: 30, 1: 40
};
for (const method of [String.prototype.charCodeAt, String.prototype.codePointAt,
    String.prototype.indexOf, String.prototype.lastIndexOf, Array.prototype.at]) {
    holder.method = method as NumericMethod;
    console.log(holder.method(), holder.method(1), holder.method(2, 1));
}
const callbacks: NumericMethod[] = [() => 7, (first = 3) => first + 1,
    (first = 3, second = 4) => first + second];
for (const callback of callbacks) {
    holder.method = callback;
    console.log(holder.method(), holder.method(2), holder.method(2, 5));
}
const arrayHolder = {method: Array.prototype.at as NumericMethod, length: 2, 0: 11, 1: 22};
console.log(arrayHolder.method(), arrayHolder.method(-1), arrayHolder.method(0, 99));
class Numbers extends Array<number> {
    read(): number { return super.at(0)! + super.at(-1)!; }
}
class Text extends String {
    read(): number { return super.charCodeAt(0); }
}
console.log(new Numbers(11, 22).read(), new Text('abc').read());
