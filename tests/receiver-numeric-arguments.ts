const savedArguments: IArguments[] = [];
const target = {
    base: 10,
    zero(): number { savedArguments.push(arguments); return this.base + arguments.length; },
    one(value: number): number { savedArguments.push(arguments); return this.base + arguments.length + value; },
    two(first: number, second: number): number { savedArguments.push(arguments); return this.base + arguments.length + first + second; }
};
function zero(value: typeof target): number { return value.zero(); }
function one(value: {one(amount: number): number}, amount: number): number { return value.one(amount); }
function two(value: typeof target, first: number, second: number): number { return value.two(first, second); }
console.log(zero(target), one(target, 2), two(target, 3, 4));
console.log(one(target, 5), two(target, 6, 7));
for (const args of savedArguments) console.log(args.length, args[0], args[1]);
console.log(savedArguments[1] !== savedArguments[3]);
savedArguments[1][0] = 99;
console.log(savedArguments[1][0], savedArguments[3][0]);
const inherited = {one(value: number): number { return value + 1; }};
const child = { __proto__: inherited, base: 20,
    one(value: number): number { return super.one(value) + this.base + arguments.length; }
};
console.log(one(child, 8));
const recorded: number[] = [];
const recorder = {base: 30, record(value: number): void { recorded.push(this.base, arguments.length, value); }};
function record(receiver: {record(value: number): void}, value: number): void { receiver.record(value); }
record(recorder, 4);
function observe(this: unknown, value: number): void { console.log('function receiver', this === undefined, arguments.length, value); }
function visit(callback: (value: number) => void, value: number): void { callback(value); }
visit(observe, 5);
console.log('method receiver', recorded.join(','));
