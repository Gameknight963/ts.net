const value = {
    number: 3,
    read(): number { return this.number; }
};
function read(receiver: {read(): number}): number { return receiver.read(); }
console.log(read(value));
value.number = -0;
console.log(Object.is(read(value), -0));
value.number = NaN;
console.log(Number.isNaN(read(value)));
value.number = Infinity;
console.log(read(value));
value.number = undefined as any;
console.log(read(value));
const other = {number: 11, extra: true};
console.log(value.read.call(other));
const accessor = {get number(): number { console.log('get'); return 17; }};
console.log(value.read.call(accessor));
const inherited = Object.create({number: 19});
console.log(value.read.call(inherited));
class Holder { number = 23; }
console.log(value.read.call(new Holder()));
const copied = {...{number: 31}};
console.log(value.read.call(copied));
Object.assign(copied, {number: 37});
console.log(value.read.call(copied));
const dictionary: Record<string, number> = {number: 41};
console.log(value.read.call(dictionary as any));
const indexed = {'0': 43, number: 47};
console.log(value.read.call(indexed));
