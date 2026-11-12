function make(offset: number) {
    return {base: 10,
        zero(): number { return this.base + offset; },
        one(value = 7): number { return this.base + offset + value; },
        two(first?: number, second?: number): number { return this.base + offset + (first ?? 0) + (second ?? 0); }
    };
}
function zero(value: {zero(): number}): number { return value.zero(); }
function one(value: {one(first?: number): number}): number { return value.one(); }
function two0(value: {two(first?: number, second?: number): number}): number { return value.two(); }
function two1(value: {two(first?: number, second?: number): number}): number { return value.two(2); }
function two2(value: {two(first?: number, second?: number): number}): number { return value.two(2, 3); }
const target = make(5);
console.log(zero(target), one(target), two0(target), two1(target), two2(target));
console.log(target.one.length, target.two.length);
function free(this: any, value = 2): number { return (this?.base ?? 3) + value; }
function call0(fn: () => number): number { return fn(); }
function call1(fn: (value: number) => number): number { return fn(4); }
function call2(fn: (first: number, second: number) => number): number { return fn(4, 9); }
console.log(call0(free), call1(free), call2(free));
const base = {base: 100, read(value: number): number {return this.base + value;}};
function child(offset: number) { return {__proto__: base, base: 10, read(value: number): number {return super.read(value) + offset;}}; }
function read(receiver: {read(value: number): number}): number { return receiver.read(1); }
const instance = child(5);
console.log(read(instance));
const alternate = {base: 30, read: instance.read};
console.log(read(alternate));
Object.setPrototypeOf(instance, {read(value: number): number {return (this as any).base * 2 + value;}});
console.log(read(alternate));
const extracted = target.one;
try { console.log(extracted(1)); } catch (error) {console.log('missing receiver', error instanceof TypeError);}
function aware(this: any, value: number): number { return new.target ? 1 : value; }
console.log(call1(aware), new (aware as any)(4) instanceof aware);
const rest = {base: 10, total(...values: number[]): number {return this.base + values.length;}};
console.log(rest.total(1, 2, 3));
function state(initial: number) {
    let offset = initial;
    return {base: 10, change(value: number): void {offset = value;}, read(value: number): number {return this.base + offset + value;}};
}
const changing = state(2);
console.log(read(changing));
changing.change(8);
console.log(read(changing));
const lexicalArguments = {base: 10, read(value: number): number {const count = () => arguments.length; return this.base + value + count();}};
console.log(read(lexicalArguments));
const bound = target.one.bind({base: 20}, 3);
console.log(call0(bound), call1(bound));
const soft = {base: 10, read(value = 2): number {return (this?.base ?? 3) + value;}};
const unbound = soft.read;
console.log(call0(unbound), call1(unbound), call2(unbound));
